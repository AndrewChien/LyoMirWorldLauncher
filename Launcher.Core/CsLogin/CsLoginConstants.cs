namespace Launcher.Core.CsLogin;

public static class CsLoginConstants
{
	public const int BufferSize = 10000;
	public const int DefBlockSize = 16;

	public const ushort CM_PROTOCOL = 2000;
	public const ushort CM_IDPASSWORD = 2001;
	public const ushort CM_ADDNEWUSER = 2002;
	public const ushort CM_CHANGEPASSWORD = 2003;
	public const ushort CM_UPDATEUSER = 2004;
	public const ushort CM_GETBACKPASSWORD = 2005;

	public const ushort SM_CERTIFICATION_SUCCESS = 500;
	public const ushort SM_CERTIFICATION_FAIL = 501;
	public const ushort SM_ID_NOTFOUND = 502;
	public const ushort SM_PASSWD_FAIL = 503;
	public const ushort SM_NEWID_SUCCESS = 504;
	public const ushort SM_NEWID_FAIL = 505;
	public const ushort SM_CHGPASSWD_SUCCESS = 506;
	public const ushort SM_CHGPASSWD_FAIL = 507;
	public const ushort SM_GETBACKPASSWD_SUCCESS = 508;
	public const ushort SM_GETBACKPASSWD_FAIL = 509;
}

