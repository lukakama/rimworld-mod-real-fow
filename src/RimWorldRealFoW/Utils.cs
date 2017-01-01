using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RimworldFoW.src.RimWorldRealFoW {
	class Utils {
		public static T getInstancePrivateValue<T>(object _this, string fieldName) {
			return (T)_this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_this);
		}

		public static void setInstancePrivateValue(object _this, string fieldName, object value) {
			_this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_this, value);
		}

		public static T execInstancePrivate<T>(object _this, string methodName, params object[] values) {
			return (T)_this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}

		public static void execInstancePrivate(object _this, string methodName, params object[] values) {
			_this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}
	}
}
