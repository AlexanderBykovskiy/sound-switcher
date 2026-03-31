using System.Runtime.InteropServices;
using SoundSwitcher.Models;

namespace SoundSwitcher.Services;

public sealed class AudioDeviceService
{
    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator
            ?? throw new InvalidOperationException("MMDeviceEnumerator is unavailable.");
        enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.Active, out var collection);
        collection.GetCount(out var count);

        var result = new List<AudioDeviceInfo>((int)count);
        for (uint i = 0; i < count; i++)
        {
            collection.Item(i, out var device);
            device.GetId(out var id);
            result.Add(new AudioDeviceInfo
            {
                Id = id,
                Name = GetFriendlyName(device)
            });
        }

        return result;
    }

    public string? GetDefaultOutputDeviceId()
    {
        var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator
            ?? throw new InvalidOperationException("MMDeviceEnumerator is unavailable.");
        enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
        device.GetId(out var id);
        return id;
    }

    public void SetDefaultOutputDevice(string deviceId)
    {
        var policyConfig = new PolicyConfigClientComObject() as IPolicyConfig
            ?? throw new InvalidOperationException("PolicyConfig is unavailable.");
        policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
        policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
        policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
    }

    private static string GetFriendlyName(IMMDevice device)
    {
        device.OpenPropertyStore(StorageAccessMode.Read, out var store);
        var key = PropertyKeys.PKEY_Device_FriendlyName;
        store.GetValue(ref key, out var value);
        try
        {
            return value.GetValue() ?? "Unknown output device";
        }
        finally
        {
            value.Clear();
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject;

    [ComImport]
    [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    private class PolicyConfigClientComObject;
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
    int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    int GetDevice(string pwstrId, out IMMDevice device);
    int RegisterEndpointNotificationCallback(IntPtr client);
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    int GetCount(out uint count);
    int Item(uint nDevice, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    int Activate(ref Guid iid, int dwClsCtx, IntPtr activationParams, out object interfacePointer);
    int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore properties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    int GetState(out DeviceState state);
}

[ComImport]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    int GetCount(out int propertyCount);
    int GetAt(int propertyIndex, out PropertyKey key);
    int GetValue(ref PropertyKey key, out PropVariant value);
    int SetValue(ref PropertyKey key, ref PropVariant value);
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
