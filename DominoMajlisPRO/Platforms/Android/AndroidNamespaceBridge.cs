#if ANDROID
namespace DominoMajlisPRO.Platforms.Android;

internal static class Android
{
    internal static class Opengl
    {
        internal static class Matrix
        {
            public static void SetIdentityM(float[] sm, int smOffset) =>
                global::Android.Opengl.Matrix.SetIdentityM(sm, smOffset);

            public static void RotateM(float[] m, int mOffset, float a, float x, float y, float z) =>
                global::Android.Opengl.Matrix.RotateM(m, mOffset, a, x, y, z);

            public static void MultiplyMM(
                float[] result,
                int resultOffset,
                float[] lhs,
                int lhsOffset,
                float[] rhs,
                int rhsOffset) =>
                global::Android.Opengl.Matrix.MultiplyMM(result, resultOffset, lhs, lhsOffset, rhs, rhsOffset);
        }
    }
}
#endif
