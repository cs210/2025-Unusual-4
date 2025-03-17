using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;

public class StreamingDownloadHandler : DownloadHandlerScript
{
    private readonly Action<string> onChunkReceived;
    private string buffer = "";  // Accumulate partial data here

    public StreamingDownloadHandler(Action<string> onChunkReceived)
        : base(new byte[1024])
    {
        this.onChunkReceived = onChunkReceived;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength <= 0)
            return false;

        // 1. Convert received bytes to string and append to buffer
        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
        buffer += chunk;

        // 2. Split the buffer by newlines or some delimiter
        //    Each "data: {...}" line ends with "\n" or "\r\n"
        //    We'll look for lines that start with "data:"
        int newlineIndex;
        while ((newlineIndex = buffer.IndexOf("\n")) >= 0)
        {
            // Extract everything up to the newline
            string line = buffer.Substring(0, newlineIndex).TrimEnd('\r');
            buffer = buffer.Substring(newlineIndex + 1); // remove that line from the buffer

            if (!string.IsNullOrEmpty(line))
            {
                // This line should be "data: {...}" or "data: [DONE]"
                onChunkReceived?.Invoke(line);
            }
        }

        return true;
    }

    protected override float GetProgress() => 0f;
}