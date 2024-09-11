using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;

public class WebRequestTester : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://127.0.0.1";
    [SerializeField] private RequestConfig[] requestConfigs;
    [SerializeField] private int requestsPerBatch = 100;
    [SerializeField] private float delayBetweenRequests = 0.1f;
    [SerializeField] private float delayBetweenBatches = 1f;
    [SerializeField] private float logInterval = 5f;

    private CancellationTokenSource cts;
    private int totalRequestsSent = 0;
    private int totalSuccessfulRequests = 0;
    private int totalFailedRequests = 0;
    private int currentConfigIndex = 0;

    public bool ShouldSendRequests { get; set; } = true;

    [System.Serializable]
    public class RequestConfig
    {
        public string endpoint;
        public RequestType requestType;
        public string postData;
    }

    public enum RequestType
    {
        GET,
        POST
    }

    public async UniTask StartContinuousTest()
    {
        cts = new CancellationTokenSource();
        ResetStats();

        try
        {
            var logTask = LogStatsPeriodicAsync(cts.Token);

            while (!cts.IsCancellationRequested)
            {
                if (ShouldSendRequests)
                {
                    await SendRequestsBatch(cts.Token);
                }
                await UniTask.Delay((int)(delayBetweenBatches * 1000), cancellationToken: cts.Token);
            }

            await logTask;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Continuous testing stopped");
        }
        finally
        {
            cts.Dispose();
            LogStats();
        }
    }

    public void StopTest()
    {
        cts?.Cancel();
    }

    private async UniTask SendRequestsBatch(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();

        for (int i = 0; i < requestsPerBatch; i++)
        {
            tasks.Add(SendSingleRequest(cancellationToken));

            if (delayBetweenRequests > 0)
            {
                await UniTask.Delay((int)(delayBetweenRequests * 1000), cancellationToken: cancellationToken);
            }
        }

        await UniTask.WhenAll(tasks);
        Debug.Log($"Batch of {requestsPerBatch} requests completed");
    }

    private async UniTask SendSingleRequest(CancellationToken cancellationToken)
    {
        RequestConfig currentConfig = requestConfigs[currentConfigIndex];
        string fullUrl = $"{baseUrl}/{currentConfig.endpoint}";

        try
        {
            using (UnityWebRequest request = CreateRequest(fullUrl, currentConfig))
            {
                totalRequestsSent++;
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    totalFailedRequests++;
                    Debug.LogError($"Request failed for {currentConfig.endpoint}: {request.error}");
                }
                else
                {
                    totalSuccessfulRequests++;
                    Debug.Log($"Request successful for {currentConfig.endpoint}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            totalFailedRequests++;
            Debug.LogError($"Error in request for {currentConfig.endpoint}: {e.Message}");
        }

        currentConfigIndex = (currentConfigIndex + 1) % requestConfigs.Length;
    }

    private UnityWebRequest CreateRequest(string url, RequestConfig config)
    {
        switch (config.requestType)
        {
            case RequestType.GET:
                return UnityWebRequest.Get(url);
            case RequestType.POST:
                byte[] bodyRaw = Encoding.UTF8.GetBytes(config.postData);
                UnityWebRequest request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                return request;
            default:
                throw new ArgumentException("Unsupported request type");
        }
    }

    private async UniTask LogStatsPeriodicAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UniTask.Delay((int)(logInterval * 1000), cancellationToken: cancellationToken);
            LogStats();
        }
    }

    private void LogStats()
    {
        Debug.Log($"Total Requests Sent: {totalRequestsSent}, " +
                  $"Successful: {totalSuccessfulRequests}, " +
                  $"Failed: {totalFailedRequests}");
    }

    private void ResetStats()
    {
        totalRequestsSent = 0;
        totalSuccessfulRequests = 0;
        totalFailedRequests = 0;
        currentConfigIndex = 0;
    }

    private void OnDisable()
    {
        StopTest();
    }
}