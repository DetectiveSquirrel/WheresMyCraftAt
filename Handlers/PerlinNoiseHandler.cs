﻿using System;

namespace WheresMyCraftAt.Handlers;

public static class PerlinNoiseHandler
{
    // Permutation table.
    private static readonly int[] permutation =
    [
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219,
        203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230,
        220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173,
        186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154,
        163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179,
        162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72,
        243, 141, 128, 195, 78, 66, 215, 61, 156, 180, 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146,
        158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135,
        130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
        223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
        251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
        138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
    ];

    // This method returns a Perlin noise value between -1 and 1.
    // For simplicity, we assume a 2D noise and use only x and y coordinates.
    public static float Generate(float x, float y)
    {
        var X = (int)Math.Floor(x) & 255; // FIND UNIT CUBE THAT CONTAINS POINT
        var Y = (int)Math.Floor(y) & 255; // FIND UNIT CUBE THAT CONTAINS POINT
        x -= (float)Math.Floor(x);        // FIND RELATIVE X,Y,Z OF POINT IN CUBE
        y -= (float)Math.Floor(y);        // FIND RELATIVE X,Y,Z OF POINT IN CUBE
        var u = Fade(x);                  // COMPUTE FADE CURVES FOR EACH OF X,Y,Z
        var v = Fade(y);                  // COMPUTE FADE CURVES FOR EACH OF X,Y,Z

        // HASH COORDINATES OF THE 8 CUBE CORNERS
        var A = permutation[X] + Y;
        var AA = permutation[A] % 256;
        var AB = permutation[A + 1] % 256;
        var B = permutation[X + 1] + Y;
        var BA = permutation[B] % 256;
        var BB = permutation[B + 1] % 256;

        // AND ADD BLENDED RESULTS FROM 8 CORNERS OF CUBE
        return Lerp(v, Lerp(u, Grad(permutation[AA], x, y), Grad(permutation[BA], x - 1, y)), Lerp(u, Grad(permutation[AB], x, y - 1), Grad(permutation[BB], x - 1, y - 1)));
    }

    private static float Fade(float t) =>
        // FADE FUNCTION AS DEFINED BY KEN PERLIN
        // 6t^5 - 15t^4 + 10t^3
        t * t * t * (t * (t * 6 - 15) + 10);

    private static float Lerp(float t, float a, float b) =>
        // LINEAR INTERPOLATE
        a + t * (b - a);

    private static float Grad(int hash, float x, float y)
    {
        // CONVERT LO 4 BITS OF HASH CODE INTO 12 GRADIENT DIRECTIONS
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h is 12 or 14 ? x : 0;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}