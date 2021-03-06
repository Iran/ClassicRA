﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Classic
{
	class CarpetBombInfo : TraitInfo<CarpetBomb>
	{
		[WeaponReference]
		public readonly string Weapon = null;
		public readonly int Range = 3;
	}

	class CarpetBomb : ITick			// todo: maybe integrate this better with the normal weapons system?
	{
		CPos Target;
		int dropDelay;

		public void SetTarget(CPos targetCell) { Target = targetCell; }

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<CarpetBombInfo>();

			if( !Combat.IsInRange( self.CenterLocation, info.Range, Target.ToPPos() ) )
				return;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return;

			if (--dropDelay <= 0)
			{
				var weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
				dropDelay = weapon.ROF;

				var args = new ProjectileArgs
				{
					srcAltitude = self.Trait<IMove>().Altitude,
					destAltitude = 0,
					src = self.CenterLocation,
					dest = self.CenterLocation,
					facing = self.Trait<IFacing>().Facing,
					firedBy = self,
					weapon = weapon
				};

				self.World.Add(args.weapon.Projectile.Create(args));

				if (!string.IsNullOrEmpty(args.weapon.Report))
					Sound.Play(args.weapon.Report + ".aud", self.CenterLocation);
			}
		}
	}
}
