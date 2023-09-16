let mediaRecorder;
let recordedData;
let isRecording = false;

function base64ToBuffer(base64String) {
  // Node:
  if (typeof Buffer !== 'undefined') {
    return Buffer.from(base64String, 'base64');
  }

  // Browser
  if (typeof atob !== 'undefined') {
    const binaryString = atob(base64String);
    const buffer = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      buffer[i] = binaryString.charCodeAt(i);
    }
    return buffer;
  }
}

// 更新按钮状态
function updateButtonState() {
  let el = document.getElementById('recordingBtn');
  el.innerHTML = isRecording ? '录音中...' : '开始对话';
}

// 开始录音
function startRecording() {
  if (!isRecording) {
    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then((stream) => {
        // 调用麦克风录音
        mediaRecorder = new MediaRecorder(stream, {
          mimeType: 'audio/webm',
        });
        mediaRecorder.addEventListener('dataavailable', function (event) {
          if (event.data.size > 0) {
            recordedData = event.data;
          }
        });
        mediaRecorder.start();
        isRecording = true;
        recordedData = null;
        updateButtonState(); // 更新按钮状态
      })
      .catch((error) => {
        console.error('获取麦克风权限失败:', error);
      });
  }
}

// 停止录音
async function stopRecording() {
  if (isRecording) {
    mediaRecorder.stop();
    isRecording = false;
    updateButtonState(); // 更新按钮状态
    setTimeout(() => {
      if (!recordedData) {
        return;
      }
      createAudioPlay();
    }, 100);
  }
}

// 生成音频
function createAudioPlay() {
  // 获取音频播放器元素
  const audio = document.querySelector('audio');
  // audio.muted = true;

  // 监听
  audio.addEventListener('canplay', () => {
    audio.play();
  });

  // 创建媒体源  https://developer.mozilla.org/zh-CN/docs/Web/API/Media_Source_Extensions_API
  const mediaSource = new MediaSource();
  audio.src = URL.createObjectURL(mediaSource);
  mediaSource.addEventListener('sourceopen', sourceOpen, { once: true });

  async function sourceOpen() {
    URL.revokeObjectURL(audio.src);
    const sourceBuffer = mediaSource.addSourceBuffer('audio/mpeg;');

    // 初始化队列和标志位
    const messageQueue = [];
    let isProcessing = false;

    async function processQueue() {
      while (messageQueue.length > 0) {
        const data = messageQueue.shift(); // 获取队列中的下一个数据
        await pushDataToSource(sourceBuffer, base64ToBuffer(data.base64)); // 处理数据
      }
      mediaSource.endOfStream();
    }

    // push Source Data
    function pushDataToSource(source, buffer) {
      return new Promise((resolve) => {
        source.appendBuffer(buffer);
        source.addEventListener(
          'updateend',
          () => {
            source.removeEventListener('updateend', resolve);
            resolve();
          },
          { once: true },
        );
      });
    }

    const url = 'https://batata.cuby.fun/api/voice-chat';
    const blob = new Blob([recordedData], { type: 'audio/webm' }); // 录音音频转成 blob
    const formData = new FormData();
    formData.append('file', blob, 'audio.mpeg');
    // formData.append('language', 'zh-CN'); 指定识别语言 可选
    const requestOptions = {
      method: 'POST',
      headers: {
        // Batata-Auth 设备端调用 API的 鉴权token 先MOCK,  传入 任一 uuid字符串 将解析成 uuid 用户
        'Batata-Auth': 'xxxx', // 其他非 uuid字字符串 将被固定解析成： 26294e3-e9f6-4292-b2f6-27167ecd5f19
      },
      body: formData,
    };
    const response = await fetch(url, requestOptions); // 请求 batata 业务 API
    const controller = new AbortController(); // 创建中断控制器 https://developer.mozilla.org/zh-CN/docs/Web/API/AbortController
    const stream = new Stream(response, controller); //  SSE 迭代器的 构建类 Stream
    console.log(stream, '=======================');
    for await (const sse of stream) {
      const { event, data, raw } = sse; // event: 事件 , data: 数据体, raw: 解析原文
      console.log(sse);
      if (event === 'audio_buffer_chunk_event') {
        // audio chunk 将接收到的 data 添加到队列
        messageQueue.push(data);
        if (!isProcessing) {
          // 如果没有正在处理的数据，则开始处理队列
          isProcessing = true;
          await processQueue();
          isProcessing = false;
        }
      } else if (event === 'action_event') {
        console.log(data.action);
        L2Dwidget.alert(data.action); // 执行 action
      }
    }
    // stream.controller.abort() 执行中断
  }
}
