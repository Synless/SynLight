namespace SynLight.MonitorState
{
    public interface IMonitorStateService
    {
        bool IsMonitorOn { get; }
        event Action<bool> MonitorStateChanged;
    }
    public class MonitorStateService : IMonitorStateService
    {
        private readonly PowerBroadcastListener _listener;

        public bool IsMonitorOn { get; private set; } = true;

        public event Action<bool> MonitorStateChanged;

        public MonitorStateService()
        {
            _listener = new PowerBroadcastListener();
            _listener.MonitorStateChanged += OnMonitorStateChanged;
        }

        private void OnMonitorStateChanged(bool isOn)
        {
            IsMonitorOn = isOn;
            MonitorStateChanged?.Invoke(isOn);
        }
    }
    public static class Services
    {
        public static IMonitorStateService MonitorState { get; } = new MonitorStateService();
    }
}