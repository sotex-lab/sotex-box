using System.Net;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace persistence.Converters;

public class IpAddressValueConverter : ValueConverter<IPAddress, byte[]>
{
    public IpAddressValueConverter()
        : base(ip => ip.GetAddressBytes(), bytes => new IPAddress(bytes)) { }
}
