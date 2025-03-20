# GuidRVAGen
A source generator that generates RVAs for GUIDs using a `GuidAttribute` equivalent.

![NuGet Version](https://img.shields.io/nuget/v/Dongle.GuidRVAGen)

[Jump to usage](#usage)

## Purpose
Typically, static GUIDs are defined using `static readonly` fields, like below:
```cs
static readonly Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
```

While this is a fine solution, it can be a bit more cumbersome to use in case of doing lower-level COM interop (which is required if you want to use CsWin32 with Native AOT).
Under CsWin32's "disable-marshalling" mode (which must be used for AOT compilation), it generates a `QueryInterface` method which takes a `Guid*`:
```cs
HRESULT QueryInterface(Guid* riid, void** ppv);
```

This means to use it with `static readonly Guid`, you would have to copy it to another variable:
```cs
IUnknown* comObject;
IUnknown* queriedObject;
Guid guid = IID_IUnknown;
Marshal.ThrowExceptionForHR(comObject->QueryInterface(&guid, (void**)(&queriedObject)));
```

Or pin it yourself:
```cs
IUnknown* comObject;
IUnknown* queriedObject;

fixed (Guid* pGuid = IID_IUnknown)
{
  Marshal.ThrowExceptionForHR(comObject->QueryInterface(pGuid, (void**)(&queriedObject)));
}
```

Or use `Marshal.QueryInterface`, which even though gives an idiomatic way to pass in the field using `ref`, requires a lot of casting:
```cs
IUnknown* comObject;
IUnknown* queriedObject;

Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)comObject, ref IUnknown, out nint pQueriedObject);
queriedObject = (IUnknown*)pQueriedObject;
```

This is not to mention that having lots of `static readonly` fields can impact performance, due to the fact that they have to be initialized all at once.

**GuidRVAGen helps resolve those problems for you!**

## Usage
To start with, you will need the following:
- .NET 9 SDK Preview 6 or newer
> [!NOTE]
> 
> You don't need to use the .NET 9 target framework itself. You only need to have the SDK installed.
- Visual Studio 2022, version 17.12 or newer (or another IDE with .NET 9 Preview 6 support or newer)
- The C# version to be set to 13.0 or newer
  - You don't need to change it in .NET 9 or newer unless you have explicitly changed the `<LangVersion>` MSBuild property.
    For older targets, add the following line to one of your `<PropertyGroup>`:
    ```xml
    <LangVersion>13.0</LangVersion>
    ```

Then, install the NuGet package and add a class, like the following:
```cs
namespace YourNamespace;

public static unsafe partial class IID
{
  [GuidRVAGen.Guid("00000000-0000-0000-c000-000000000046")]
  public static partial Guid* IID_IUnknown { get; }
}
```
> [!IMPORTANT]
>
> The `partial` keyword is critical. Make sure you set it to the property, along with any types that the property resides in (in this case, the `IID` class).

> [!CAUTION]
>
> Do **NOT** change the value of the `Guid*` that the property returns! Doing so can cause crashes, memory corruption, etc.

As you can see, we now have a `Guid*` property that we can now use:
```cs
IUnknown* comObject;
IUnknown* queriedObject;

Marshal.ThrowExceptionForHR(comObject->QueryInterface(IID.IID_IUnknown, (void**)(&queriedObject)));
```

If you need a `ref` or `ref readonly` return value instead, you can also do that!
```cs
namespace YourNamespace;

public static unsafe partial class IID
{
  [GuidRVAGen.Guid("00000000-0000-0000-c000-000000000046")]
  public static partial ref readonly Guid IID_IUnknown { get; }
}
```
```cs
namespace YourNamespace;

public static unsafe partial class IID
{
  [GuidRVAGen.Guid("00000000-0000-0000-c000-000000000046")]
  public static partial ref Guid IID_IUnknown { get; }
}
```
> [!CAUTION]
>
> Similar to the `Guid*` return type, do **NOT** change the value of the `ref` returned!
