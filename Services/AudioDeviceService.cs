using System.Runtime.InteropServices;
using SoundSwitcher.Models;

namespace SoundSwitcher.Services;

public sealed class AudioDeviceService : IDisposable
{
    private readonly IMMDeviceEnumerator _enumerator;
    private readonly AudioEndpointNotificationClient _notificationClient;
    private bool _disposed;

    public event EventHandler? DevicesChanged;

    public AudioDeviceService()
    {
        _enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator
            ?? throw new InvalidOperationException("MMDeviceEnumerator is unavailable.");
        _notificationClient = new AudioEndpointNotificationClient(OnAudioNotification);
        try
        {
            var hr = _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        catch
        {
            _ = Marshal.ReleaseComObject(_enumerator);
            throw;
        }
    }

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IMMDeviceCollection? collection = null;
        try
        {
            var hr = _enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.Active, out collection);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            ArgumentNullException.ThrowIfNull(collection);
            var endpoints = collection;
            endpoints.GetCount(out var count);
            var result = new List<AudioDeviceInfo>((int)count);
            for (uint i = 0; i < count; i++)
            {
                IMMDevice? device = null;
                try
                {
                    hr = endpoints.Item(i, out device);
                    if (hr != 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    ArgumentNullException.ThrowIfNull(device);
                    var endpoint = device;
                    endpoint.GetId(out var id);
                    result.Add(new AudioDeviceInfo
                    {
                        Id = id,
                        Name = GetFriendlyName(endpoint)
                    });
                }
                finally
                {
                    if (device is not null)
                    {
                        _ = Marshal.ReleaseComObject(device);
                    }
                }
            }

            return result;
        }
        finally
        {
            if (collection is not null)
            {
                _ = Marshal.ReleaseComObject(collection);
            }
        }
    }

    public string? GetDefaultOutputDeviceId()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IMMDevice? device = null;
        try
        {
            var hr = _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            ArgumentNullException.ThrowIfNull(device);
            var defaultDevice = device;
            defaultDevice.GetId(out var id);
            return id;
        }
        finally
        {
            if (device is not null)
            {
                _ = Marshal.ReleaseComObject(device);
            }
        }
    }

    public void SetDefaultOutputDevice(string deviceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var comPolicy = new PolicyConfigClientComObject();
        var policy = (IPolicyConfig)comPolicy;
        try
        {
            policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);
        }
        finally
        {
            _ = Marshal.ReleaseComObject(comPolicy);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _ = _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        }
        catch (Exception ex)
        {
            StartupLogger.Error(ex, "Unregister audio endpoint notification callback failed.");
        }

        _ = Marshal.ReleaseComObject(_enumerator);

        GC.SuppressFinalize(this);
    }

    private void OnAudioNotification()
    {
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetFriendlyName(IMMDevice device)
    {
        IPropertyStore? store = null;
        var hr = device.OpenPropertyStore(StorageAccessMode.Read, out store);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        ArgumentNullException.ThrowIfNull(store);
        var props = store;
        try
        {
            var key = PropertyKeys.PKEY_Device_FriendlyName;
            hr = props.GetValue(ref key, out var value);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                return value.GetValue() ?? "Unknown output device";
            }
            finally
            {
                value.Clear();
            }
        }
        finally
        {
            _ = Marshal.ReleaseComObject(props);
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject;

    [ComImport]
    [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    private class PolicyConfigClientComObject;
}

/// <summary>
/// Колбэки MMDevice не должны блокироваться; поднимаем событие в пуле потоков.
/// </summary>
internal sealed class AudioEndpointNotificationClient : IMMNotificationClient
{
    private readonly Action _onChange;

    public AudioEndpointNotificationClient(Action onChange) => _onChange = onChange;

    public int OnDeviceStateChanged(string pwstrDeviceId, uint dwNewState) => NotifyLater();

    public int OnDeviceAdded(string pwstrDeviceId) => NotifyLater();

    public int OnDeviceRemoved(string pwstrDeviceId) => NotifyLater();

    public int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? pwstrDefaultDeviceId) => NotifyLater();

    public int OnPropertyValueChanged(string pwstrDeviceId, ref PropertyKey key) => NotifyLater();

    private int NotifyLater()
    {
        ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), _onChange);
        return 0;
    }
}

[ComImport]
[Guid("7991ECE9-7E31-456D-9F27-948390F03E28")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient
{
    [PreserveSig]
    int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, uint dwNewState);

    [PreserveSig]
    int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);

    [PreserveSig]
    int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);

    [PreserveSig]
    int OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string? pwstrDefaultDeviceId);

    [PreserveSig]
    int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, ref PropertyKey key);
}

internal static class PropertyKeys
{
    internal static PropertyKey PKEY_Device_FriendlyName => new(
        new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey(Guid fmtid, int pid)
{
    public Guid Fmtid = fmtid;
    public int Pid = pid;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant
{
    [FieldOffset(0)] private ushort _valueType;
    [FieldOffset(8)] private IntPtr _ptr;

    public string? GetValue() =>
        _valueType == (ushort)VarEnum.VT_LPWSTR ? Marshal.PtrToStringUni(_ptr) : null;

    public void Clear() => _ = PropVariantClear(ref this);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection? devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice? endpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice? device);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IMMNotificationClient client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint nDevice, out IMMDevice? device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr activationParams, out object interfacePointer);

    [PreserveSig]
    int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore? properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

    [PreserveSig]
    int GetState(out DeviceState state);
}

[ComImport]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out int propertyCount);

    [PreserveSig]
    int GetAt(int propertyIndex, out PropertyKey key);

    [PreserveSig]
    int GetValue(ref PropertyKey key, out PropVariant value);

    [PreserveSig]
    int SetValue(ref PropertyKey key, ref PropVariant value);

    [PreserveSig]
    int Commit();
}

/// <summary>
/// Порядок методов как в рабочем interop EarTrumpet (иначе vtable не совпадает и SetDefaultEndpoint падает с 0x8007065E).
/// </summary>
[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    void Unused1();
    void Unused2();
    void Unused3();
    void Unused4();
    void Unused5();
    void Unused6();
    void Unused7();
    void Unused8();
    void GetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId,
        ref PropertyKey pkey,
        ref PropVariant pv);
    void SetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId,
        ref PropertyKey pkey,
        ref PropVariant pv);
    void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId, ERole eRole);
    void SetEndpointVisibility(
        [MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId,
        [MarshalAs(UnmanagedType.I2)] short isVisible);
}

internal enum EDataFlow
{
    eRender,
    eCapture,
    eAll,
    EDataFlowEnumCount
}

internal enum ERole
{
    eConsole,
    eMultimedia,
    eCommunications,
    ERoleEnumCount
}

[Flags]
internal enum DeviceState : uint
{
    Active = 0x00000001,
    Disabled = 0x00000002,
    NotPresent = 0x00000004,
    Unplugged = 0x00000008,
    All = 0x0000000F
}

internal enum StorageAccessMode
{
    Read = 0
}
