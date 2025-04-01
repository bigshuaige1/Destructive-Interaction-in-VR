using UnityEngine;

public class SafeStartup : MonoBehaviour
{
    // 用于标识程序是否已经初始化过
    private static bool isInitialized = false;

    private void Awake()
    {
        // 如果程序已经初始化过，则直接退出
        if (isInitialized)
        {
            Debug.LogWarning("程序已经初始化过，防止重复启动导致的崩溃");
            gameObject.SetActive(false);
            return;
        }

        // 标记程序已经初始化过
        isInitialized = true;

        // 在这里进行程序的初始化操作
        InitializeProgram();
    }

    private void InitializeProgram()
    {
        // 初始化代码
        Debug.Log("程序初始化成功");
    }

    private void OnDestroy()
    {
        // 清理代码，确保资源被释放
        CleanUp();
    }

    private void CleanUp()
    {
        // 释放资源
        Debug.Log("程序正在清理资源");
        isInitialized = false; // 重置初始化状态，以便程序可以重新启动
    }
}