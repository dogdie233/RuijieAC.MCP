using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

using RuijieAC.MCP.Models;

namespace RuijieAC.MCP.Utils;

internal static partial class CommonUtil
{
    [GeneratedRegex(@"<return-code>(\d+)</return-code>")]
    internal static partial Regex ParseReturnCodeRegex();
    
    public static X509Certificate2 LoadCertificate(string certFilePath)
    {
        if (!Path.IsPathFullyQualified(certFilePath))
            certFilePath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory,
                certFilePath);

        return X509CertificateLoader.LoadCertificateFromFile(certFilePath);
    }

    public static string LoginHmac(HmacInfo hmacInfo, string username, string password)
    {
        if (!hmacInfo.AaaEnable)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                Encoding.ASCII.GetBytes(hmacInfo.Salt),
                hmacInfo.Iter,
                HashAlgorithmName.SHA256);

            var key = pbkdf2.GetBytes(hmacInfo.KeyLen);
            return Convert.ToHexStringLower(key);
        }
        else
        {
            var idx = username.Length % 3;
            var newSalt = hmacInfo.Salt.AsSpan()[idx..^2];
            var length = Math.Max(password.Length, newSalt.Length);
            var resultStr = new string('0', length * 2);
            var result = MemoryMarshal.CreateSpan(ref Unsafe.As<char, uint>(ref Unsafe.AsRef(in resultStr.GetPinnableReference())), length);

            for (var i = 0; i < length; i++)
            {
                var dataChar = i < password.Length ? (byte)password[i] : (byte)0;
                var saltChar = i < newSalt.Length ? (byte)newSalt[i] : (byte)0;
                var xorChar = (byte)(dataChar ^ saltChar);
                result[i] = ToCharsBuffer(xorChar, Casing.Lower);
            }

            return resultStr;
        }
    }

    public static int? ParseReturnCode(string response)
    {
        var match = ParseReturnCodeRegex().Match(response);
        if (!match.Success) return null;
        
        var returnCode = match.Groups[1].Value;
        if (int.TryParse(returnCode, out var code))
            return code;

        return null;
    }

    public static string LoginReturnCode2String(int code)
        => code switch
        {
            -1 => "无法连接设备，请检查网络连接是否正常",
            11 => "输入的账户或密码错误次数达到上限，用户被锁定，请稍等10分钟再试！",
            32 or 33 => "IP已被锁定，请稍等10分钟再试！",
            36 => "该账户同时登录个数已达上限！",
            12 => "仅限admin用户登录!",
            10 => "用户名或密码错误！",
            _ => "未知的错误，可能是用户名或密码错误！"
        };
    
    
    // From https://source.dot.net/#System.Net.Primitives/src/libraries/Common/src/System/HexConverter.cs
    private enum Casing : uint
    {
        Upper = 0,
        Lower = 0x2020U,
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ToCharsBuffer(byte value, Casing casing)
    {
        var difference = BitConverter.IsLittleEndian 
            ? ((uint)value >> 4) + ((value & 0x0Fu) << 16) - 0x890089u 
            : ((value & 0xF0u) << 12) + (value & 0x0Fu) - 0x890089u;
        var packedResult = ((((uint)-(int)difference & 0x700070u) >> 4) + difference + 0xB900B9u) | (uint)casing;
        return packedResult;
    }
}