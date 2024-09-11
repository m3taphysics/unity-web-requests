using UnityEngine;
using Cysharp.Threading.Tasks;

public class WebRequestController : MonoBehaviour
{
    public WebRequestTester tester;

    [SerializeField] private bool isSendingRequests = true;

    public bool IsSendingRequests
    {
        get => isSendingRequests;
        set
        {
            isSendingRequests = value;
            if (tester != null)
            {
                tester.ShouldSendRequests = isSendingRequests;
            }
        }
    }

    private async void Start()
    {
        if (tester == null)
        {
            Debug.LogError("WebRequestTester not assigned to TestRunner!");
            return;
        }

        // Set initial state
        tester.ShouldSendRequests = IsSendingRequests;

        // Start the continuous test
        await StartContinuousTest();
    }

    public async UniTask StartContinuousTest()
    {
        if (tester != null)
        {
            await tester.StartContinuousTest();
        }
        else
        {
            Debug.LogError("WebRequestTester not assigned to TestRunner!");
        }
    }

    public void StopTest()
    {
        if (tester != null)
        {
            tester.StopTest();
        }
    }

    private void OnValidate()
    {
        // This ensures that changes in the Inspector are immediately reflected
        if (tester != null)
        {
            tester.ShouldSendRequests = IsSendingRequests;
        }
    }
}