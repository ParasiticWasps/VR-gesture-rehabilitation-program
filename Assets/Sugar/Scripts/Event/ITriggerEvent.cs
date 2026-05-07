

using System;

public interface ITriggerEvent
{
    public void OnEvent(Action callback);
}