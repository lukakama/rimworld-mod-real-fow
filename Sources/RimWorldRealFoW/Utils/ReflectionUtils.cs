//   Copyright 2017 Luca De Petrillo
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
using System;
using System.Reflection;

namespace RimWorldRealFoW.Utils {
	class ReflectionUtils {
		public static T getInstancePrivateProperty<T>(object _this, string fieldName) {
			return (T) _this.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_this, null);
		}

		public static T getInstancePrivateValue<T>(object _this, string fieldName) {
			return (T) _this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_this);
		}

		public static void setInstancePrivateValue(object _this, string fieldName, object value) {
			_this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_this, value);
		}

		public static T execInstancePrivate<T>(object _this, string methodName, params object[] values) {
			return (T) _this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}

		public static void execInstancePrivate(object _this, string methodName, params object[] values) {
			_this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}

		public static T getStaticPrivateValue<T>(Type type, string fieldName) {
			return (T) type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		}

		internal static void execStaticPrivate(Type type, string methodName, params object[] values) {
			type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, values);
		}
		internal static T execStaticPrivate<T>(Type type, string methodName, params object[] values) {
			return (T) type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, values);
		}
	}
}
