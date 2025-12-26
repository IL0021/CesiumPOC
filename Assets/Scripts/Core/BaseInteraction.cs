using System;
using UnityEngine;

public abstract class BaseInteraction : MonoBehaviour
{
    public event Action<BaseInteraction> OnComplete;
    public abstract void Process();
    protected void FinishInteraction() => OnComplete?.Invoke(this);
}