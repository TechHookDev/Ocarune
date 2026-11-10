#pragma once
// NVIDIA DLSS super resolution through NGX, in the MSVC-built bridge hs_dlss.dll (dlss/hs_dlss.cpp), loaded at run time.
// Everything is a no-op (dlss_ready() false) when the bridge, nvngx_dlss.dll or an RTX GPU is missing.
#include "vk_internal.h"
#include <vector>

namespace mkwvk {
struct HsImg { VkImageView view; VkImage image; VkFormat format; unsigned w, h; };
struct HsEval {
    HsImg color, depth, mv, out;
    unsigned in_w, in_h, out_w, out_h;
    float jx, jy, sharpness;
    int reset, quality, hdr;
};
// before vkCreateInstance / vkCreateDevice: append the extensions NGX needs (only those the driver lists)
void dlss_instance_exts(std::vector<const char*>& exts);
void dlss_device_exts(std::vector<const char*>& exts, const std::vector<VkExtensionProperties>& available);
// after vkCreateDevice
void dlss_init();
bool dlss_ready();
void dlss_eval(VkCommandBuffer cmd, const HsEval& e);
void dlss_shutdown();
}

namespace mkwvk {
struct Texture;
// the scene's depth targets (called when a pass opens with one); DLSS reads the one at the colour's size
void dlss_note_depth(const Texture* depth);
// the jitter (pixels) for geometry passes of w x h this frame; 0 when DLSS is off or the pass is not the scene's
void dlss_jitter(uint32_t w, uint32_t h, float& jx, float& jy);
struct DlssImg { VkImage image; VkImageView view; VkFormat format; uint32_t w, h; };
// records DLSS: color -> a private RGBA16F image (ow x oh, its view in result; the caller converts it into the game's target); true when it ran (the caller must then call descriptors_disturbed)
bool dlss_run(VkCommandBuffer cmd, const DlssImg& color, uint32_t ow, uint32_t oh, VkImageView& result);
bool dlss_active();   // the menu's DLSS is chosen and the bridge works
}
