using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 诊断脚本：打包后找出所有使用 OutlineOnly_URP shader 的物体
/// </summary>
public class DebugBuildWhiteObjects : MonoBehaviour
{
    void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        Debug.LogError($"=== 场景 [{scene.name}] 使用 OutlineOnly_White 材质的物体诊断开始 ===");

        int total = 0;
        int outlineCount = 0;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.scene != scene) continue;

            // 检查 MeshRenderer
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                total++;
                outlineCount += CheckMaterials(go, mr.sharedMaterials, mr);
            }

            // 检查 SkinnedMeshRenderer
            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                total++;
                outlineCount += CheckMaterials(go, smr.sharedMaterials, smr);
            }
        }

        Debug.LogError($"=== 诊断完成: 共{total}个Renderer, {outlineCount}个使用 Custom/OutlineOnly_URP ===");
    }

    int CheckMaterials(GameObject go, Material[] mats, Renderer renderer)
    {
        int count = 0;
        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null) continue;
            if (mat.shader == null) continue;
            bool isOutlineMat = mat.name.Contains("OutlineOnly_White") || mat.shader.name.Contains("OutlineOnly");
            if (!isOutlineMat) continue;

            count++;

            string meshName = "NULL";
            int vertCount = 0;
            if (renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                meshName = smr.sharedMesh.name;
                vertCount = smr.sharedMesh.vertexCount;
            }
            else
            {
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    meshName = mf.sharedMesh.name;
                    vertCount = mf.sharedMesh.vertexCount;
                }
            }

            bool parentHasOutline = go.transform.parent?.GetComponent<AddOutlineToRenderer>() != null;

            Debug.LogError($"[OutlineOnly] 物体:{go.name} | Mesh:{meshName} | 顶点:{vertCount} | 材质名:{mat.name} | 槽位:{i} | 父:{go.transform.parent?.name ?? "无"} | 父有AddOutline:{parentHasOutline} | 路径:{GetFullPath(go.transform)}");
        }
        return count;
    }

    string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }
}
