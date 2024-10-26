Console.WriteLine(Guids.SampleGuid);

public partial class Guids
{
    [GuidRVAGen.Guid("f81d4fae-7dec-11d0-a765-00a0c91e6bf6")]
    public static partial ref readonly Guid SampleGuid { get; }
}