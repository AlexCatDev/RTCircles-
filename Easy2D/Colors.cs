using System.Diagnostics.Contracts;
using System;
using System.Numerics;

namespace Easy2D
{
    public static class Colors
    {
        #region PredefinedColors

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static Vector4 Transparent { get; private set; } = new Vector4(255, 255, 255, 0) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static Vector4 AliceBlue { get; private set; } = new Vector4(240, 248, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static Vector4 AntiqueWhite { get; private set; } = new Vector4(250, 235, 215, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Vector4 Aqua { get; private set; } = new Vector4(0, 255, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static Vector4 Aquamarine { get; private set; } = new Vector4(127, 255, 212, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static Vector4 Azure { get; private set; } = new Vector4(240, 255, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static Vector4 Beige { get; private set; } = new Vector4(245, 245, 220, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static Vector4 Bisque { get; private set; } = new Vector4(255, 228, 196, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static Vector4 Black { get; private set; } = new Vector4(0, 0, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static Vector4 BlanchedAlmond { get; private set; } = new Vector4(255, 235, 205, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static Vector4 Blue { get; private set; } = new Vector4(0, 0, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static Vector4 BlueViolet { get; private set; } = new Vector4(138, 43, 226, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static Vector4 Brown { get; private set; } = new Vector4(165, 42, 42, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static Vector4 BurlyWood { get; private set; } = new Vector4(222, 184, 135, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static Vector4 CadetBlue { get; private set; } = new Vector4(95, 158, 160, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static Vector4 Chartreuse { get; private set; } = new Vector4(127, 255, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static Vector4 Chocolate { get; private set; } = new Vector4(210, 105, 30, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static Vector4 Coral { get; private set; } = new Vector4(255, 127, 80, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static Vector4 CornflowerBlue { get; private set; } = new Vector4(100, 149, 237, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static Vector4 Cornsilk { get; private set; } = new Vector4(255, 248, 220, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static Vector4 Crimson { get; private set; } = new Vector4(220, 20, 60, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Vector4 Cyan { get; private set; } = new Vector4(0, 255, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static Vector4 DarkBlue { get; private set; } = new Vector4(0, 0, 139, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static Vector4 DarkCyan { get; private set; } = new Vector4(0, 139, 139, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static Vector4 DarkGoldenrod { get; private set; } = new Vector4(184, 134, 11, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static Vector4 DarkGray { get; private set; } = new Vector4(169, 169, 169, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static Vector4 DarkGreen { get; private set; } = new Vector4(0, 100, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static Vector4 DarkKhaki { get; private set; } = new Vector4(189, 183, 107, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static Vector4 DarkMagenta { get; private set; } = new Vector4(139, 0, 139, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static Vector4 DarkOliveGreen { get; private set; } = new Vector4(85, 107, 47, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static Vector4 DarkOrange { get; private set; } = new Vector4(255, 140, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static Vector4 DarkOrchid { get; private set; } = new Vector4(153, 50, 204, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static Vector4 DarkRed { get; private set; } = new Vector4(139, 0, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static Vector4 DarkSalmon { get; private set; } = new Vector4(233, 150, 122, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139, 255).
        /// </summary>
        public static Vector4 DarkSeaGreen { get; private set; } = new Vector4(143, 188, 139, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static Vector4 DarkSlateBlue { get; private set; } = new Vector4(72, 61, 139, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static Vector4 DarkSlateGray { get; private set; } = new Vector4(47, 79, 79, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static Vector4 DarkTurquoise { get; private set; } = new Vector4(0, 206, 209, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static Vector4 DarkViolet { get; private set; } = new Vector4(148, 0, 211, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static Vector4 DeepPink { get; private set; } = new Vector4(255, 20, 147, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static Vector4 DeepSkyBlue { get; private set; } = new Vector4(0, 191, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static Vector4 DimGray { get; private set; } = new Vector4(105, 105, 105, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static Vector4 DodgerBlue { get; private set; } = new Vector4(30, 144, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static Vector4 Firebrick { get; private set; } = new Vector4(178, 34, 34, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static Vector4 FloralWhite { get; private set; } = new Vector4(255, 250, 240, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static Vector4 ForestGreen { get; private set; } = new Vector4(34, 139, 34, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Vector4 Fuchsia { get; private set; } = new Vector4(255, 0, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static Vector4 Gainsboro { get; private set; } = new Vector4(220, 220, 220, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static Vector4 GhostWhite { get; private set; } = new Vector4(248, 248, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static Vector4 Gold { get; private set; } = new Vector4(255, 215, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static Vector4 Goldenrod { get; private set; } = new Vector4(218, 165, 32, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static Vector4 Gray { get; private set; } = new Vector4(128, 128, 128, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static Vector4 Green { get; private set; } = new Vector4(0, 128, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static Vector4 GreenYellow { get; private set; } = new Vector4(173, 255, 47, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static Vector4 Honeydew { get; private set; } = new Vector4(240, 255, 240, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static Vector4 HotPink { get; private set; } = new Vector4(255, 105, 180, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static Vector4 IndianRed { get; private set; } = new Vector4(205, 92, 92, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static Vector4 Indigo { get; private set; } = new Vector4(75, 0, 130, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static Vector4 Ivory { get; private set; } = new Vector4(255, 255, 240, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static Vector4 Khaki { get; private set; } = new Vector4(240, 230, 140, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static Vector4 Lavender { get; private set; } = new Vector4(230, 230, 250, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static Vector4 LavenderBlush { get; private set; } = new Vector4(255, 240, 245, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static Vector4 LawnGreen { get; private set; } = new Vector4(124, 252, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static Vector4 LemonChiffon { get; private set; } = new Vector4(255, 250, 205, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static Vector4 LightBlue { get; private set; } = new Vector4(173, 216, 230, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static Vector4 LightCoral { get; private set; } = new Vector4(240, 128, 128, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static Vector4 LightCyan { get; private set; } = new Vector4(224, 255, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static Vector4 LightGoldenrodYellow { get; private set; } = new Vector4(250, 250, 210, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static Vector4 LightGreen { get; private set; } = new Vector4(144, 238, 144, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static Vector4 LightGray { get; private set; } = new Vector4(211, 211, 211, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static Vector4 LightPink { get; private set; } = new Vector4(255, 182, 193, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static Vector4 LightSalmon { get; private set; } = new Vector4(255, 160, 122, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static Vector4 LightSeaGreen { get; private set; } = new Vector4(32, 178, 170, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static Vector4 LightSkyBlue { get; private set; } = new Vector4(135, 206, 250, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static Vector4 LightSlateGray { get; private set; } = new Vector4(119, 136, 153, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static Vector4 LightSteelBlue { get; private set; } = new Vector4(176, 196, 222, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static Vector4 LightYellow { get; private set; } = new Vector4(255, 255, 224, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static Vector4 Lime { get; private set; } = new Vector4(0, 255, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static Vector4 LimeGreen { get; private set; } = new Vector4(50, 205, 50, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static Vector4 Linen { get; private set; } = new Vector4(250, 240, 230, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Vector4 Magenta { get; private set; } = new Vector4(255, 0, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static Vector4 Maroon { get; private set; } = new Vector4(128, 0, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static Vector4 MediumAquamarine { get; private set; } = new Vector4(102, 205, 170, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static Vector4 MediumBlue { get; private set; } = new Vector4(0, 0, 205, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static Vector4 MediumOrchid { get; private set; } = new Vector4(186, 85, 211, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static Vector4 MediumPurple { get; private set; } = new Vector4(147, 112, 219, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static Vector4 MediumSeaGreen { get; private set; } = new Vector4(60, 179, 113, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static Vector4 MediumSlateBlue { get; private set; } = new Vector4(123, 104, 238, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static Vector4 MediumSpringGreen { get; private set; } = new Vector4(0, 250, 154, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static Vector4 MediumTurquoise { get; private set; } = new Vector4(72, 209, 204, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static Vector4 MediumVioletRed { get; private set; } = new Vector4(199, 21, 133, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static Vector4 MidnightBlue { get; private set; } = new Vector4(25, 25, 112, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static Vector4 MintCream { get; private set; } = new Vector4(245, 255, 250, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static Vector4 MistyRose { get; private set; } = new Vector4(255, 228, 225, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static Vector4 Moccasin { get; private set; } = new Vector4(255, 228, 181, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static Vector4 NavajoWhite { get; private set; } = new Vector4(255, 222, 173, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static Vector4 Navy { get; private set; } = new Vector4(0, 0, 128, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static Vector4 OldLace { get; private set; } = new Vector4(253, 245, 230, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static Vector4 Olive { get; private set; } = new Vector4(128, 128, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static Vector4 OliveDrab { get; private set; } = new Vector4(107, 142, 35, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static Vector4 Orange { get; private set; } = new Vector4(255, 165, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static Vector4 OrangeRed { get; private set; } = new Vector4(255, 69, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static Vector4 Orchid { get; private set; } = new Vector4(218, 112, 214, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static Vector4 PaleGoldenrod { get; private set; } = new Vector4(238, 232, 170, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static Vector4 PaleGreen { get; private set; } = new Vector4(152, 251, 152, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static Vector4 PaleTurquoise { get; private set; } = new Vector4(175, 238, 238, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static Vector4 PaleVioletRed { get; private set; } = new Vector4(219, 112, 147, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static Vector4 PapayaWhip { get; private set; } = new Vector4(255, 239, 213, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static Vector4 PeachPuff { get; private set; } = new Vector4(255, 218, 185, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static Vector4 Peru { get; private set; } = new Vector4(205, 133, 63, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static Vector4 Pink { get; private set; } = new Vector4(255, 192, 203, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static Vector4 Plum { get; private set; } = new Vector4(221, 160, 221, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static Vector4 PowderBlue { get; private set; } = new Vector4(176, 224, 230, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static Vector4 Purple { get; private set; } = new Vector4(128, 0, 128, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static Vector4 Red { get; private set; } = new Vector4(255, 0, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static Vector4 RosyBrown { get; private set; } = new Vector4(188, 143, 143, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static Vector4 RoyalBlue { get; private set; } = new Vector4(65, 105, 225, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static Vector4 SaddleBrown { get; private set; } = new Vector4(139, 69, 19, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static Vector4 Salmon { get; private set; } = new Vector4(250, 128, 114, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static Vector4 SandyBrown { get; private set; } = new Vector4(244, 164, 96, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static Vector4 SeaGreen { get; private set; } = new Vector4(46, 139, 87, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static Vector4 SeaShell { get; private set; } = new Vector4(255, 245, 238, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static Vector4 Sienna { get; private set; } = new Vector4(160, 82, 45, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static Vector4 Silver { get; private set; } = new Vector4(192, 192, 192, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static Vector4 SkyBlue { get; private set; } = new Vector4(135, 206, 235, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static Vector4 SlateBlue { get; private set; } = new Vector4(106, 90, 205, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static Vector4 SlateGray { get; private set; } = new Vector4(112, 128, 144, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static Vector4 Snow { get; private set; } = new Vector4(255, 250, 250, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static Vector4 SpringGreen { get; private set; } = new Vector4(0, 255, 127, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static Vector4 SteelBlue { get; private set; } = new Vector4(70, 130, 180, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static Vector4 Tan { get; private set; } = new Vector4(210, 180, 140, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static Vector4 Teal { get; private set; } = new Vector4(0, 128, 128, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static Vector4 Thistle { get; private set; } = new Vector4(216, 191, 216, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static Vector4 Tomato { get; private set; } = new Vector4(255, 99, 71, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static Vector4 Turquoise { get; private set; } = new Vector4(64, 224, 208, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static Vector4 Violet { get; private set; } = new Vector4(238, 130, 238, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static Vector4 Wheat { get; private set; } = new Vector4(245, 222, 179, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static Vector4 White { get; private set; } = new Vector4(255, 255, 255, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static Vector4 WhiteSmoke { get; private set; } = new Vector4(245, 245, 245, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static Vector4 Yellow { get; private set; } = new Vector4(255, 255, 0, 255) * SCALE255;

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static Vector4 YellowGreen { get; private set; } = new Vector4(154, 205, 50, 255) * SCALE255;

        #endregion

        private const float SCALE255 = 0.00392156862f;

        public static Vector3 From255RGB(float r, float g, float b) => new Vector3(r, g, b) * SCALE255;
        public static Vector4 From255RGBA(float r, float g, float b, float a) => new Vector4(r, g, b, a) * SCALE255;

        public static Vector4 Tint(Vector4 color, float maxValue)
        {
            float ratio = 1.0f;
            if(color.X > color.Y && color.X > color.Z)
                ratio = maxValue / color.X;
            else if (color.Y > color.X && color.Y > color.Z)
                ratio = maxValue / color.Y;
            else if (color.Z > color.X && color.Z > color.Y)
                ratio = maxValue / color.Z;
            else
                ratio = maxValue / color.X;

            color.X *= ratio;
            color.Y *= ratio;
            color.Z *= ratio;

            return color;
        }

        /// <summary>
        /// Converts HSV color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hsv">
        /// Color value to convert in hue, saturation, value (HSV).
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V), and the W element is Alpha
        /// (which is copied to the output's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        [Pure]
        public static Vector3 FromHsv(Vector3 hsv)
        {
            var hue = hsv.X * 360.0f;
            var saturation = hsv.Y;
            var value = hsv.Z;

            var c = value * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs((h % 2.0f) - 1.0f));

            float r, g, b;
            if (h >= 0.0f && h < 1.0f)
            {
                r = c;
                g = x;
                b = 0.0f;
            }
            else if (h >= 1.0f && h < 2.0f)
            {
                r = x;
                g = c;
                b = 0.0f;
            }
            else if (h >= 2.0f && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = x;
            }
            else if (h >= 3.0f && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = c;
            }
            else if (h >= 4.0f && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = c;
            }
            else if (h >= 5.0f && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = value - c;
            return new Vector3(r + m, g + m, b + m);
        }
    }
}
