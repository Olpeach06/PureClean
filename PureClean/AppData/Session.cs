using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureClean.AppData
{
    public static class Session
    {
        internal static int CartItemCount;

        public static int? UserID { get; set; }
        public static string Login { get; set; }
        public static string FullName { get; set; }
        public static int RoleID { get; set; }
        public static DateTime LoginTime { get; set; }

        public static bool IsGuest => RoleID == 0;
        public static bool IsAuthenticated => UserID.HasValue && !IsGuest;
        public static bool IsAdmin => RoleID == 4;
        public static bool IsManager => RoleID == 3;
        public static bool IsUser => RoleID == 2;

        public static void InitializeAsGuest()
        {
            UserID = null;
            Login = "Гость";
            FullName = "Гость";
            RoleID = 0;
            LoginTime = DateTime.Now;
        }

        public static void Clear()
        {
            UserID = null;
            Login = null;
            FullName = null;
            RoleID = 0;
        }
    }
}
