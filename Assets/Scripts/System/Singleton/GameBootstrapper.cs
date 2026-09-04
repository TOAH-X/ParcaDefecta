using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ParcaDefecta.System
{
    /// <summary>
    /// ゲーム起動時に一度だけ実行される初期化クラス。
    /// Singleton&lt;T&gt; を継承した具象クラスを全アセンブリから自動収集して生成します。
    /// 新しいマネージャーは Singleton&lt;T&gt; を継承するだけで生成対象になり、ここへの追記は不要です。
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var singletonTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => (type: t, singletonBase: FindSingletonBase(t)))
                .Where(x => x.singletonBase != null)
                .OrderBy(x => x.type.Name); // 動作順序は不問。起動ログの並びを安定させるためだけの並べ替え

            int count = 0;
            foreach (var (type, singletonBase) in singletonTypes)
            {
                // Singleton<具象型>.Instance を参照することで、Singleton.cs 側のロジックで
                // 「個別のGameObject作成」と「共通の親への紐付け」を自動実行させます。
                singletonBase.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                Debug.Log($"[GameBootstrapper] {type.Name} を生成しました");
                count++;
            }

            Debug.Log($"[GameBootstrapper] シングルトン {count} 件の生成が完了しました");
        }

        /// <summary>
        /// 基底を辿り、Singleton&lt;T&gt; の閉じたジェネリック型を返します。継承していなければ null。
        /// </summary>
        private static Type FindSingletonBase(Type type)
        {
            for (var t = type.BaseType; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Singleton<>))
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// 一部の型を読み込めないアセンブリがあっても、読み込めた型だけを返します。
        /// </summary>
        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}
