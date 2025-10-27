using UnityEngine;

/// <summary>
/// 幾何学系の関数
/// </summary>
public static class GeometryMath
{
    /// <summary>
    /// 三角形と三角形の交差判定
    /// </summary>
    public static bool IsTriangleIntersectTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 u0, Vector3 u1, Vector3 u2)
    {
        // 三角形1の各辺が三角形2と交差しているか
        if (LineIntersectsTriangle(v0, v1, u0, u1, u2)) return true;
        if (LineIntersectsTriangle(v1, v2, u0, u1, u2)) return true;
        if (LineIntersectsTriangle(v2, v0, u0, u1, u2)) return true;

        // 三角形2の各辺が三角形1と交差しているか
        if (LineIntersectsTriangle(u0, u1, v0, v1, v2)) return true;
        if (LineIntersectsTriangle(u1, u2, v0, v1, v2)) return true;
        if (LineIntersectsTriangle(u2, u0, v0, v1, v2)) return true;

        // どの辺も交差していない
        return false;
    }

    /// <summary>
    /// 線分と三角形の交差判定
    /// </summary>
    public static bool LineIntersectsTriangle(Vector3 from, Vector3 to, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        const float EPSILON = 1e-8f;

        Vector3 dir = to - from;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        // 三角形法線との関係を計算
        Vector3 pvec = Vector3.Cross(dir, edge2);
        float det = Vector3.Dot(edge1, pvec);

        // 平行判定
        if (Mathf.Abs(det) < EPSILON)
            return false;

        float invDet = 1.0f / det;
        Vector3 tvec = from - v0;

        // バリュー u の計算（三角形内部判定用）
        float u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < 0.0f || u > 1.0f)
            return false;

        // バリュー v の計算（三角形内部判定用）
        Vector3 qvec = Vector3.Cross(tvec, edge1);
        float v = Vector3.Dot(dir, qvec) * invDet;
        if (v < 0.0f || u + v > 1.0f)
            return false;

        // 線分上の交差位置
        float t = Vector3.Dot(edge2, qvec) * invDet;

        // t が [0,1] の範囲内なら線分上で交差
        return (t >= 0.0f && t <= 1.0f);
    }

    /// <summary>
    /// レイと三角形の交差判定
    /// </summary>
    public static bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDir, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
    {
        const float EPSILON = 1e-8f;
        t = 0f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(rayDir, edge2);
        float a = Vector3.Dot(edge1, h);

        if (a > -EPSILON && a < EPSILON) return false; // レイと三角形が平行

        float f = 1.0f / a;
        Vector3 s = rayOrigin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f) return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(rayDir, q);
        if (v < 0.0f || u + v > 1.0f) return false;

        // レイが三角形平面と交差
        t = f * Vector3.Dot(edge2, q); return t > EPSILON;
    }
}
