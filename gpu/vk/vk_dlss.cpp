#include <cstdlib>
#include "vk_dlss.h"
#include "vk_internal.h"
#include <windows.h>
#include <cstring>

namespace mkwvk {
namespace {
using RequiredFn = int (*)(unsigned*, const char***, unsigned*, const char***);
using InitFn = int (*)(VkInstance, VkPhysicalDevice, VkDevice, PFN_vkGetInstanceProcAddr, PFN_vkGetDeviceProcAddr, const wchar_t*);
using EvalFn = int (*)(VkCommandBuffer, const HsEval*);
using ShutFn = void (*)();
HMODULE g_mod;
RequiredFn g_required;
InitFn g_init;
EvalFn g_eval;
ShutFn g_shut;
bool g_ok;

bool load() {
    if (g_mod) return g_required != nullptr;
    if (getenv("RX_VK_NO_DLSS")) return false;
    g_mod = LoadLibraryA("hs_dlss.dll");   // next to the exe
    if (!g_mod) { logf("vk: dlss: hs_dlss.dll not found"); return false; }
    g_required = (RequiredFn)(void*)GetProcAddress(g_mod, "hs_dlss_required");
    g_init = (InitFn)(void*)GetProcAddress(g_mod, "hs_dlss_init");
    g_eval = (EvalFn)(void*)GetProcAddress(g_mod, "hs_dlss_eval");
    g_shut = (ShutFn)(void*)GetProcAddress(g_mod, "hs_dlss_shutdown");
    if (!g_required || !g_init || !g_eval || !g_shut) g_required = nullptr;
    return g_required != nullptr;
}
}

void dlss_instance_exts(std::vector<const char*>& exts) {
    unsigned ic = 0, dc = 0;
    const char **ie = nullptr, **de = nullptr;
    if (!load() || !g_required(&ic, &ie, &dc, &de)) return;
    uint32_t n = 0;
    vkEnumerateInstanceExtensionProperties(nullptr, &n, nullptr);
    std::vector<VkExtensionProperties> all(n);
    vkEnumerateInstanceExtensionProperties(nullptr, &n, all.data());
    for (unsigned i = 0; i < ic; i++) {
        bool have = false, dup = false;
        for (const auto& e : all) have |= !std::strcmp(e.extensionName, ie[i]);
        for (const char* x : exts) dup |= !std::strcmp(x, ie[i]);
        if (have && !dup) exts.push_back(ie[i]);
    }
}

void dlss_device_exts(std::vector<const char*>& exts, const std::vector<VkExtensionProperties>& available) {
    unsigned ic = 0, dc = 0;
    const char **ie = nullptr, **de = nullptr;
    if (!g_required || !g_required(&ic, &ie, &dc, &de)) return;
    for (unsigned i = 0; i < dc; i++) {
        bool have = false, dup = false;
        for (const auto& e : available) have |= !std::strcmp(e.extensionName, de[i]);
        for (const char* x : exts) dup |= !std::strcmp(x, de[i]);
        if (have && !dup) exts.push_back(de[i]);
    }
}

void dlss_init() {
    if (!g_required) return;
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(g_mod, path, MAX_PATH);   // (hs_dlss.dll's folder: nvngx_dlss.dll sits beside it)
    if (wchar_t* s = wcsrchr(path, (wchar_t)92)) *s = 0;
    g_ok = g_init(dev.instance, dev.physical, dev.device, vkx::instance_proc_addr(), vkGetDeviceProcAddr, path) != 0;
    logf("vk: dlss: %s", g_ok ? "ready" : "unavailable (needs an RTX GPU, a current driver and nvngx_dlss.dll)");
}

bool dlss_ready() { return g_ok; }
void dlss_eval(VkCommandBuffer cmd, const HsEval& e) { if (g_ok) g_eval(cmd, &e); }
void dlss_shutdown() { if (g_ok && g_shut) g_shut(); g_ok = false; }
}

#include "vk_menu.h"
#include "vk_texture.h"
#include <mutex>
#include <map>

namespace mkwvk {
namespace {
struct DepthRec { VkImage image; VkFormat format; uint32_t w, h; uint64_t gen; };
std::map<VkImage, VkImageView> g_depth_views;   // (sampled depth-aspect views; leaked with a destroyed image: rare)
std::mutex g_mu;
std::map<uint64_t, DepthRec> g_depth;   // (by size)
uint32_t g_scene_w, g_scene_h;           // (the colour size of the last run: the passes to jitter)
uint64_t g_last_frame;
struct Priv { VkImage image = VK_NULL_HANDLE; VmaAllocation mem = nullptr; VkImageView view = VK_NULL_HANDLE; uint32_t w = 0, h = 0; };
Priv g_mv, g_out;   // (zero motion; DLSS's own RGBA16F output: it takes no B10G11R11)

float halton(uint32_t i, uint32_t b) {
    float f = 1.0f, r = 0.0f;
    for (i++; i; i /= b) f /= (float)b, r += f * (float)(i % b);
    return r;
}
void jitter_now(float& jx, float& jy) {
    static const bool flip = getenv("RX_VK_DLSS_FLIP") != nullptr;
    const uint32_t i = (uint32_t)(texture_frame_no() % 16);
    jx = halton(i, 2) - 0.5f, jy = halton(i, 3) - 0.5f;
    if (flip) jx = -jx, jy = -jy;
}
// a private image (zero-cleared) at a size, for DLSS's motion vectors (RG16F) and output (RGBA16F)
bool ensure_img(VkCommandBuffer cmd, Priv& g, VkFormat fmt, VkImageUsageFlags usage, uint32_t w, uint32_t h) {
    if (g.image && g.w == w && g.h == h) return true;
    if (g.image) {
        vkDeviceWaitIdle(dev.device);
        vkDestroyImageView(dev.device, g.view, nullptr);
        destroy_image(g.image, g.mem);
        g = {};
    }
    VkImageCreateInfo ci{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};
    ci.imageType = VK_IMAGE_TYPE_2D, ci.format = fmt, ci.extent = {w, h, 1};
    ci.mipLevels = ci.arrayLayers = 1, ci.samples = VK_SAMPLE_COUNT_1_BIT, ci.tiling = VK_IMAGE_TILING_OPTIMAL;
    ci.usage = usage | VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_STORAGE_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    if (!create_image(ci, g.image, g.mem)) { g = {}; return false; }
    VkImageViewCreateInfo vi{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};
    vi.image = g.image, vi.viewType = VK_IMAGE_VIEW_TYPE_2D, vi.format = fmt;
    vi.subresourceRange = {VK_IMAGE_ASPECT_COLOR_BIT, 0, 1, 0, 1};
    vkCreateImageView(dev.device, &vi, nullptr, &g.view);
    VkImageMemoryBarrier2 b{VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER_2};
    b.srcStageMask = b.dstStageMask = VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    b.dstAccessMask = VK_ACCESS_2_MEMORY_READ_BIT | VK_ACCESS_2_MEMORY_WRITE_BIT;
    b.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED, b.newLayout = VK_IMAGE_LAYOUT_GENERAL;
    b.srcQueueFamilyIndex = b.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
    b.image = g.image, b.subresourceRange = vi.subresourceRange;
    VkDependencyInfo di{VK_STRUCTURE_TYPE_DEPENDENCY_INFO};
    di.imageMemoryBarrierCount = 1, di.pImageMemoryBarriers = &b;
    vkCmdPipelineBarrier2(cmd, &di);
    const VkClearColorValue zero{};
    vkCmdClearColorImage(cmd, g.image, VK_IMAGE_LAYOUT_GENERAL, &zero, 1, &vi.subresourceRange);
    g.w = w, g.h = h;
    return true;
}
}

bool dlss_active() { return g_ok && opts.upscale == 4 /* (no menu entry now: kept for RX_VK_DLSS=1 experiments) */ && getenv("RX_VK_DLSS"); }

void dlss_note_depth(const Texture* t) {
    const uint32_t w = t->width, h = t->height;
    if (!dlss_active() || w < 640 || w == h || !(t->usage & VK_IMAGE_USAGE_SAMPLED_BIT)) return;
    std::lock_guard lk(g_mu);
    g_depth[(uint64_t)w << 32 | h] = {t->image, t->format, w, h, texture_destroy_generation()};
}

void dlss_jitter(uint32_t w, uint32_t h, float& jx, float& jy) {
    jx = jy = 0.0f;
    if (dlss_active() && w == g_scene_w && h == g_scene_h) jitter_now(jx, jy);
}

bool dlss_run(VkCommandBuffer cmd, const DlssImg& color, uint32_t ow, uint32_t oh, VkImageView& result) {
    if (!dlss_active()) return false;
    DepthRec d;
    {
        std::lock_guard lk(g_mu);
        auto it = g_depth.find((uint64_t)color.w << 32 | color.h);
        if (it == g_depth.end() || it->second.gen != texture_destroy_generation()) {
            static int told;
            if (told++ < 3) logf("vk: dlss: no depth target of %ux%u yet", color.w, color.h);
            g_scene_w = color.w, g_scene_h = color.h;   // (so the next frame's passes are jittered and noted)
            return false;
        }
        d = it->second;
    }
    if (!ensure_img(cmd, g_mv, VK_FORMAT_R16G16_SFLOAT, 0, color.w, color.h) ||
        !ensure_img(cmd, g_out, VK_FORMAT_R16G16B16A16_SFLOAT, 0, ow, oh)) return false;
    VkImageView &dv = g_depth_views[d.image];
    if (!dv) {
        VkImageViewUsageCreateInfo uc{VK_STRUCTURE_TYPE_IMAGE_VIEW_USAGE_CREATE_INFO};
        uc.usage = VK_IMAGE_USAGE_SAMPLED_BIT;
        VkImageViewCreateInfo vi{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO, &uc};
        vi.image = d.image, vi.viewType = VK_IMAGE_VIEW_TYPE_2D, vi.format = d.format;
        vi.subresourceRange = {VK_IMAGE_ASPECT_DEPTH_BIT, 0, 1, 0, 1};
        if (vkCreateImageView(dev.device, &vi, nullptr, &dv) != VK_SUCCESS) { dv = VK_NULL_HANDLE; return false; }
    }
    static const bool hdr = !getenv("RX_VK_DLSS_SDR");
    HsEval e{};
    e.color = {color.view, color.image, color.format, color.w, color.h};
    e.out = {g_out.view, g_out.image, VK_FORMAT_R16G16B16A16_SFLOAT, ow, oh};
    e.depth = {dv, d.image, d.format, d.w, d.h};
    e.mv = {g_mv.view, g_mv.image, VK_FORMAT_R16G16_SFLOAT, color.w, color.h};
    e.in_w = color.w, e.in_h = color.h, e.out_w = ow, e.out_h = oh;
    jitter_now(e.jx, e.jy);
    const uint64_t now = texture_frame_no();
    e.reset = (now - g_last_frame > 3 || g_scene_w != color.w || g_scene_h != color.h) ? 1 : 0;
    g_last_frame = now, g_scene_w = color.w, g_scene_h = color.h;
    const float ratio = (float)color.w / (float)ow;   // (NGX perf/quality enum: 5 DLAA, 4 UltraQuality ~0.77, 2 Quality 0.667, 1 Balanced, 0 MaxPerf)
    e.quality = ratio > 0.95f ? 5 : ratio > 0.62f ? 2 : ratio > 0.5f ? 1 : 0;
    e.hdr = hdr;
    full_barrier(cmd);
    const int r = g_eval(cmd, &e);
    full_barrier(cmd);
    static bool logged;
    static int failed;
    if (r != 1) {   // (NGX result codes are 0xBAD000xx: the call did nothing, so the caller's own upscaler runs)
        if (failed++ < 5) logf("vk: dlss: NGX call failed, result %#x (colour fmt %d, depth fmt %d, out %ux%u)", (unsigned)r, (int)color.format, (int)d.format, ow, oh);
        return false;
    }
    if (!logged) logged = true, logf("vk: dlss: running %ux%u -> %ux%u, quality %d, depth format %d", color.w, color.h, ow, oh, e.quality, (int)d.format);
    result = g_out.view;
    return true;
}
}
