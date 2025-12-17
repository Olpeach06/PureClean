using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureClean.AppData
{
    public static class CurrentUser
    {
        public static int UserID { get; set; }
        public static string Login { get; set; }
        public static string FullName { get; set; }
        public static int RoleID { get; set; }
        public static DateTime LoginTime { get; set; }

        public static void Clear()
        {
            UserID = 0;
            Login = string.Empty;
            FullName = string.Empty;
            RoleID = 0;
            LoginTime = DateTime.MinValue;
        }

        public static bool IsAdmin => RoleID == 4;
        public static bool IsManager => RoleID == 3 || RoleID == 4;
        public static bool IsUser => RoleID >= 2;
        public static bool IsGuest => RoleID == 1;
    }
}
