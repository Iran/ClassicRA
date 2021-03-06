﻿#region Copyright & License Information
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
using System.Text;
using OpenRA.Mods.RA.Classic.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Classic.AI
{
    class VehicleBuilder : AIBuilder, IAIBuilder
    {
        enum BuildState { ChooseItem, WaitForProduction, WaitForFeedback }
        BuildState state = BuildState.WaitForFeedback;

        //       public readonly string[] UnitQueues = { "Vehicle", "Infantry", "Plane", "Ship" };
        int feedbacktime = 60; // time to update feedback state, in ticks

        public VehicleBuilder(IranAI AI)
        {
            this.AI = AI;
            this.world = AI.world;
        }

        public void Tick()
        {
            // Pick a free queue
            var queue = FindQueues("Vehicle").FirstOrDefault();
            if (queue == null)
            {
                AI.Debug("Can't find free queue.");
                return;
            }

            switch (state)
            {
                case BuildState.ChooseItem:
                    {
                        if (queue.CurrentItem() != null)
                        {
//                            AI.Debug("VehicleBuilder currentItem != null");
                            state = BuildState.WaitForProduction;
                            return;
                        }

                        String item = ChooseVehicle(queue);
                        if (item == null)
                        {
                            state = BuildState.WaitForFeedback;
                            //                            AI.Debug("No building to choose from to produce");
                        }
                        else
                        {
                            state = BuildState.WaitForProduction;
                            world.IssueOrder(Order.StartProduction(queue.self, item, 1));
//                            AI.Debug("Ordering vehicle {0}", item);
                        }
                        break;
                    }
                case BuildState.WaitForProduction:
                    {
                        var currentBuilding = queue.CurrentItem();

                        if (currentBuilding == null)
                        {
                            state = BuildState.WaitForFeedback;
                            /*AI.Debug("currentBuilding is null");*/
                            return;
                        }

                        else if (currentBuilding.Paused)
                            world.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));

                        if (queue.CurrentDone)
                        {
                            state = BuildState.WaitForFeedback;
                            //                           PlaceStructure(queue, currentBuilding);
                        }
                        break;
                    }
                case BuildState.WaitForFeedback:
                    {
                        if (AI.ticks % feedbacktime == 0)
                            state = BuildState.ChooseItem;
                        break;
                    }
            }
        }

        String ChooseVehicle(ProductionQueue queue)
        {
            String item = null;

            if (AI.p.Country.Race == "soviet")
            {
                int random = AI.random.Next() % 4;

                if (random == 0)
                {
                    item = "3tnk";
                }
                else if (random == 1)
                {
                    item = "4tnk";
                }
                else if (random == 2)
                {
                    item = "ttnk";
                }
                else if (random == 3)
                {
                    item = "v2rl";
                }
            }

            else if (AI.p.Country.Race == "allies")
            {
                int random = AI.random.Next() % 5;

                if (random == 0)
                {
                    item = "1tnk";
                }
                else if (random == 1)
                {
                    item = "2tnk";
                }
                else if (random == 2)
                {
                    item = "apc";
                }
                else if (random == 3)
                {
                    item = "arty";
                }
                else if (random == 4)
                {
                    item = "jeep";
                }
            }

            var buildableThings = queue.BuildableItems();
            if (buildableThings.Any(b => b.Name == item)) return item; // Return it only if we can actually build it
            return null;
        }
    }
}

