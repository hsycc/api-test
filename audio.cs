using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class SimulatedAudioStream : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip audioClip;
    private List<float> audioBuffer = new List<float>();
    private bool isPlaying = false;

    void Start()
    {
        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();

        // 启动协程以模拟从远程API获取MP3音频流
        StartCoroutine(SimulateRemoteAPI());
    }

    // 模拟从远程API获取MP3音频流的协程
    IEnumerator SimulateRemoteAPI()
    {
        string apiUrl = "https://aigc-hub.cuby.fun/api/speech/text-to-speech?text=%E9%82%A3%E6%88%91%E5%B8%8C%E6%9C%9B%E6%AF%8F%E5%A4%A9%E9%83%BD%E8%83%BD%E5%90%83%E5%88%B0%E4%B8%96%E7%95%8C%E4%B8%8A%E6%9C%80%E5%A5%BD%E5%90%83%E7%9A%84%E4%B8%9C%E8%A5%BF%EF%BC%81%E2%80%9D%E7%B2%BE%E7%81%B5%E7%AC%91%E7%9D%80%E8%AF%B4%EF%BC%9A%E2%80%9C%E5%A5%BD%E7%9A%84%EF%BC%8C%E4%BB%8E%E6%98%8E%E5%A4%A9%E5%BC%80%E5%A7%8B%EF%BC%8C%E4%BD%A0%E6%AF%8F%E5%A4%A9%E9%83%BD%E8%83%BD%E5%90%83%E5%88%B0%E4%B8%96%E7%95%8C%E4%B8%8A%E6%9C%80%E5%A5%BD%E5%90%83%E7%9A%84%E4%B8%9C%E8%A5%BF%E2%80%94%E2%80%94%E5%A6%88%E5%A6%88%E5%81%9A%E7%9A%84%E9%A5%AD%E8%8F%9C%EF%BC%81%E2%80%9D%E5%B0%8F%E6%98%8E%E9%A1%BF%E6%97%B6%E6%97%A0%E8%A8%80%E4%BB%A5%E5%AF%B9%EF%BC%8C%E5%BF%83%E6%83%B3%EF%BC%8C%E5%A6%88%E5%A6%88%E5%81%9A%E7%9A%84%E9%A5%AD%E8%8F%9C%E5%A5%BD%E5%90%83%E6%98%AF%E5%A5%BD%E5%90%83%EF%BC%8C%E4%BD%86%E6%AF%8F%E5%A4%A9%E5%90%83%E4%B9%9F%E5%A4%AA%E8%BE%9B%E8%8B%A6%E4%BA%86%E5%90%A7%EF%BC%81&voiceName=zh-CN-XiaoyiNeural&outputFormat=3&provider=azure";
        string apiKey = "sk-a31acfd4-9e097e2-290413f-c49a790-9feea26-37b766a";

        using (HttpClient client = new HttpClient())
        {
            // 添加请求头
            client.DefaultRequestHeaders.Add("accept", "*");
            client.DefaultRequestHeaders.Add("Cubyfun-Auth", apiKey);

            try
            {
                using (var response = await client.GetStreamAsync(apiUrl))
                using (var reader = new System.IO.BinaryReader(response))
                {
                    // 读取并处理音频数据流
                    byte[] buffer = new byte[4096]; // 调整缓冲区大小
                    int bytesRead;
                    bool firstChunkReceived = false;

                    while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // 如果还没有开始播放，开始播放
                        if (!isPlaying)
                        {
                            isPlaying = true;

                            // 创建音频剪辑
                            audioClip = AudioClip.Create("CustomAudio", audioBuffer.Count, 1, 44100, false); // 这里假设采样率为 44100
                            audioClip.SetData(audioBuffer.ToArray(), 0);

                            // 设置音频源的音频剪辑
                            audioSource.clip = audioClip;

                            // 设置音频源开始播放的延迟，可以是缓冲的时间
                            float bufferTime = (float)audioBuffer.Count / audioSource.clip.frequency;
                            audioSource.PlayDelayed(bufferTime);
                        }

                        // 处理读取到的音频数据
                        for (int i = 0; i < bytesRead; i++)
                        {
                            audioBuffer.Add((float)buffer[i] / 255f); // 将字节转换为浮点数并归一化到 [0, 1] 范围内
                        }

                        // 标记第一段 chunk 已接收
                        if (!firstChunkReceived)
                        {
                            firstChunkReceived = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("发生异常：" + ex.Message);
            }
        }

        yield return null;
    }
}
