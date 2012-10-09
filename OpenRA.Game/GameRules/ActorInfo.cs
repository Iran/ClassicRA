#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public class ActorInfo
	{
		public readonly string Name;
		public readonly TypeDictionary Traits = new TypeDictionary();

		public ActorInfo( string name, MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			try
			{
				var mergedNode = MergeWithParent(node, allUnits).NodesDict;

				Name = name;
				foreach (var t in mergedNode)
					if (t.Key != "Inherits" && !t.Key.StartsWith("-"))
						Traits.Add(LoadTraitInfo(t.Key.Split('@')[0], t.Value));
			}
			catch (YamlException e)
			{
				throw new YamlException("Actor type {0}: {1}".F(name, e.Message));
			}
		}

		static MiniYaml GetParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			MiniYaml inherits;
			node.NodesDict.TryGetValue( "Inherits", out inherits );
			if( inherits == null || string.IsNullOrEmpty( inherits.Value ) )
				return null;

			MiniYaml parent;
			allUnits.TryGetValue( inherits.Value, out parent );
			if (parent == null)
				throw new InvalidOperationException(
					"Bogus inheritance -- actor type {0} does not exist".F(inherits.Value));

			return parent;
		}

		static MiniYaml MergeWithParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			var parent = GetParent( node, allUnits );
			if (parent != null)
			{
				var result = MiniYaml.MergeStrict(node, MergeWithParent(parent, allUnits));

				// strip the '-'
				result.Nodes.RemoveAll(a => a.Key.StartsWith("-"));
				return result;
			}
			return node;
		}

		static ITraitInfo LoadTraitInfo(string traitName, MiniYaml my)
		{
			if (!string.IsNullOrEmpty(my.Value))
				throw new YamlException("Junk value `{0}` on trait node {1}"
				.F(my.Value, traitName));
			var info = Game.CreateObject<ITraitInfo>(traitName + "Info");
			FieldLoader.Load(info, my);
			return info;
		}

		public IEnumerable<ITraitInfo> TraitsInConstructOrder()
		{
			var ret = new List<ITraitInfo>();
			var t = Traits.WithInterface<ITraitInfo>().ToList();
			int index = 0;
			while (t.Count != 0)
			{
				var prereqs = PrerequisitesOf(t[index]);
				var unsatisfied = prereqs.Where(n => !ret.Any(x => x.GetType() == n || n.IsAssignableFrom(x.GetType())));
				if (!unsatisfied.Any())
				{
					ret.Add(t[index]);
					t.RemoveAt(index);
					index = 0;
				}
				else if (++index >= t.Count)
					throw new InvalidOperationException("Trait prerequisites not satisfied (or prerequisite loop) Actor={0} Unresolved={1} Missing={2}".F(
						Name,
						t.Select(x => x.GetType().Name).JoinWith(","),
						unsatisfied.Select(x => x.Name).JoinWith(",")));
			}

			return ret;
		}

		static List<Type> PrerequisitesOf(ITraitInfo info)
		{
			return info
				.GetType()
				.GetInterfaces()
				.Where( t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof( Requires<> ) )
				.Select( t => t.GetGenericArguments()[ 0 ] )
				.ToList();
		}

		public IEnumerable<Pair<string, Type>> GetInitKeys()
		{
			var inits = Traits.WithInterface<ITraitInfo>().SelectMany(
				t => t.GetType().GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(UsesInit<>))
					.Select(i => i.GetGenericArguments()[0])).ToList();

			inits.Add( typeof(OwnerInit) );		/* not exposed by a trait; this is used by the Actor itself */

			return inits.Select(
				i => Pair.New(
					i.Name.Replace( "Init", "" ), i ));
		}
	}
}