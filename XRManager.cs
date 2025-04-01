using UnityEngine;

public class SafeStartup : MonoBehaviour
{
    // ���ڱ�ʶ�����Ƿ��Ѿ���ʼ����
    private static bool isInitialized = false;

    private void Awake()
    {
        // ��������Ѿ���ʼ��������ֱ���˳�
        if (isInitialized)
        {
            Debug.LogWarning("�����Ѿ���ʼ��������ֹ�ظ��������µı���");
            gameObject.SetActive(false);
            return;
        }

        // ��ǳ����Ѿ���ʼ����
        isInitialized = true;

        // ��������г���ĳ�ʼ������
        InitializeProgram();
    }

    private void InitializeProgram()
    {
        // ��ʼ������
        Debug.Log("�����ʼ���ɹ�");
    }

    private void OnDestroy()
    {
        // ������룬ȷ����Դ���ͷ�
        CleanUp();
    }

    private void CleanUp()
    {
        // �ͷ���Դ
        Debug.Log("��������������Դ");
        isInitialized = false; // ���ó�ʼ��״̬���Ա���������������
    }
}