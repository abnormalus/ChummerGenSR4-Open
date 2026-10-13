# Retina framebuffer correction

Source: https://github.com/majorsilence/Majorsilence.Forms at commit 2999abd025b324890f385fa505d07d065fef30cb (26.11.4), MIT license.

Desktop backend compiled against the locally corrected 26.11.4 core source, retaining exact desktop dependency versions.

EnsureFramebuffer tags its physical-pixel bitmap at 96 DPI instead of 96 times the display scale. The stretched Image and compositor already map framebuffer pixels onto the display; the old metadata magnifies the scene again on Retina displays and separates painted controls from logical hit targets. This matches the correction already present in MajorsilenceFormsPresenter.

Interactive Retina checks: clicking the visible New toolbar icon opened Select Setting; clicking its OK button opened Choose BP Amount. The core spinner correction is documented in ../LOCAL_PATCHES.md.
