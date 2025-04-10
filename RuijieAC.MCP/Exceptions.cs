using RuijieAC.MCP.Utils;

namespace RuijieAC.MCP;

public class LoginException(int code) : Exception($"Login failed({code}): {CommonUtil.LoginReturnCode2String(code)}")
{
}