// Plain-C bridge to NVIDIA NGX DLSS (Vulkan). Built with MSVC (the NGX libs are MSVC-ABI) as hs_dlss.dll and loaded by oot_vk.dll.
#include <vulkan/vulkan.h>
#include "nvsdk_ngx_vk.h"
#include "nvsdk_ngx_helpers_vk.h"
#include <string.h>

#define EXPORT extern "C" __declspec(dllexport)

struct HsImg { VkImageView view; VkImage image; VkFormat format; unsigned w, h; };
struct HsEval {
    HsImg color, depth, mv, out;
    unsigned in_w, in_h, out_w, out_h;
    float jx, jy, sharpness;
    int reset, quality, hdr;
};

static NVSDK_NGX_Parameter* g_params;
static NVSDK_NGX_Handle* g_feature;
static unsigned g_cw, g_ch, g_ow, g_oh;
static int g_cq, g_chdr;
static VkDevice g_dev;

EXPORT int hs_dlss_required(unsigned* ic, const char*** ie, unsigned* dc, const char*** de) {
    return NVSDK_NGX_VULKAN_RequiredExtensions(ic, ie, dc, de) == NVSDK_NGX_Result_Success ? 1 : 0;
}

EXPORT int hs_dlss_init(VkInstance inst, VkPhysicalDevice pd, VkDevice dev, PFN_vkGetInstanceProcAddr gipa, PFN_vkGetDeviceProcAddr gdpa, const wchar_t* path) {
    if (NVSDK_NGX_VULKAN_Init(0x4F6F54ull, path, inst, pd, dev, gipa, gdpa, nullptr, NVSDK_NGX_Version_API) != NVSDK_NGX_Result_Success) return 0;
    g_dev = dev;
    if (NVSDK_NGX_VULKAN_GetCapabilityParameters(&g_params) != NVSDK_NGX_Result_Success) return 0;
    int ok = 0;
    NVSDK_NGX_Parameter_GetI(g_params, NVSDK_NGX_Parameter_SuperSampling_Available, &ok);
    return ok;
}

static void release() {
    if (g_feature) { NVSDK_NGX_VULKAN_ReleaseFeature(g_feature); g_feature = nullptr; }
}

static NVSDK_NGX_Resource_VK res(const HsImg& i, bool rw, VkImageAspectFlags aspect) {
    VkImageSubresourceRange r = {aspect, 0, 1, 0, 1};
    return NVSDK_NGX_Create_ImageView_Resource_VK(i.view, i.image, r, i.format, i.w, i.h, rw);
}

// returns the NGX result (1 = success); the caller must rebind its pipeline/descriptors afterwards
EXPORT int hs_dlss_eval(VkCommandBuffer cmd, const HsEval* e) {
    if (!g_params) return -1;
    if (g_feature && (g_cw != e->in_w || g_ch != e->in_h || g_ow != e->out_w || g_oh != e->out_h || g_cq != e->quality || g_chdr != e->hdr)) release();
    if (!g_feature) {
        NVSDK_NGX_DLSS_Create_Params cp = {};
        cp.Feature.InWidth = e->in_w; cp.Feature.InHeight = e->in_h;
        cp.Feature.InTargetWidth = e->out_w; cp.Feature.InTargetHeight = e->out_h;
        cp.Feature.InPerfQualityValue = (NVSDK_NGX_PerfQuality_Value)e->quality;
        cp.InFeatureCreateFlags = NVSDK_NGX_DLSS_Feature_Flags_MVLowRes | NVSDK_NGX_DLSS_Feature_Flags_AutoExposure |
                                  (e->hdr ? NVSDK_NGX_DLSS_Feature_Flags_IsHDR : 0);
        const NVSDK_NGX_Result cr = NGX_VULKAN_CREATE_DLSS_EXT1(g_dev, cmd, 1, 1, &g_feature, g_params, &cp);
        if (cr != NVSDK_NGX_Result_Success) { g_feature = nullptr; return (int)cr | 0x1000; }   // (0x1000 marks a create failure)
        g_cw = e->in_w; g_ch = e->in_h; g_ow = e->out_w; g_oh = e->out_h; g_cq = e->quality; g_chdr = e->hdr;
    }
    NVSDK_NGX_Resource_VK c = res(e->color, false, VK_IMAGE_ASPECT_COLOR_BIT), o = res(e->out, true, VK_IMAGE_ASPECT_COLOR_BIT),
                          m = res(e->mv, false, VK_IMAGE_ASPECT_COLOR_BIT), d = res(e->depth, false, VK_IMAGE_ASPECT_DEPTH_BIT);
    NVSDK_NGX_VK_DLSS_Eval_Params p = {};
    p.Feature.pInColor = &c; p.Feature.pInOutput = &o; p.Feature.InSharpness = e->sharpness;
    p.pInDepth = &d; p.pInMotionVectors = &m;
    p.InJitterOffsetX = e->jx; p.InJitterOffsetY = e->jy;
    p.InRenderSubrectDimensions = {e->in_w, e->in_h};
    p.InReset = e->reset; p.InMVScaleX = 1.0f; p.InMVScaleY = 1.0f; p.InPreExposure = 1.0f; p.InExposureScale = 1.0f;
    return (int)NGX_VULKAN_EVALUATE_DLSS_EXT(cmd, g_feature, g_params, &p);
}

EXPORT void hs_dlss_shutdown() {
    release();
    if (g_params) { NVSDK_NGX_VULKAN_DestroyParameters(g_params); g_params = nullptr; }
    if (g_dev) { NVSDK_NGX_VULKAN_Shutdown1(g_dev); g_dev = nullptr; }
}
