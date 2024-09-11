using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class WebRequestTester : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://127.0.0.1";
    [SerializeField] private string[] fileNames = { "index.html", "sample.pdf", "sample.png" };
    [SerializeField] private int requestsPerBatch = 100;
    [SerializeField] private float delayBetweenRequests = 0.1f;
    [SerializeField] private float delayBetweenBatches = 1f;
    [SerializeField] private float logInterval = 5f; // Log stats every 5 seconds

    private CancellationTokenSource cts;
    private int totalRequestsSent = 0;
    private int totalSuccessfulRequests = 0;
    private int totalFailedRequests = 0;
    private int currentFileIndex = 0;

    public bool ShouldSendRequests { get; set; } = true;

    public async UniTask StartContinuousTest()
    {
        cts = new CancellationTokenSource();
        totalRequestsSent = 0;
        totalSuccessfulRequests = 0;
        totalFailedRequests = 0;
        currentFileIndex = 0;

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
            LogStats(); // Log final stats
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
        string currentFile = fileNames[currentFileIndex];
        string fullUrl = $"{baseUrl}/{currentFile}";

        try
        {
            using (var request = UnityWebRequest.Get(fullUrl))
            {
                totalRequestsSent++;
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    totalFailedRequests++;
                    Debug.LogError($"Request failed for {currentFile}: {request.error}");
                }
                else
                {
                    totalSuccessfulRequests++;
                    Debug.Log($"Request successful for {currentFile}");
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
            Debug.LogError($"Error in request for {currentFile}: {e.Message}");
        }

        // Move to the next file
        currentFileIndex = (currentFileIndex + 1) % fileNames.Length;
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

    private void OnDisable()
    {
        StopTest();
    }
}