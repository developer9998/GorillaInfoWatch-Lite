using GorillaInfoWatch.Extensions;
using GorillaInfoWatch.Models;
using GorillaInfoWatch.Models.Attributes;
using GorillaInfoWatch.Models.Enumerations;
using GorillaInfoWatch.Models.Widgets;
using GorillaInfoWatch.Tools;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaInfoWatch.Screens
{
    [ShowOnHomeScreen]
    internal class FriendScreen : InfoScreen
    {
        public override string Title => "Friends";

        public List<FriendBackendController.Friend> FriendsList;

        public override void OnScreenLoad()
        {
            FriendSystem.Instance.OnFriendListRefresh += OnGetFriendsReceived;
            RequestFriendsList();
        }

        public override void OnScreenUnload()
        {
            FriendSystem.Instance.OnFriendListRefresh -= OnGetFriendsReceived;
        }

        public override void OnScreenReload()
        {
            RequestFriendsList();
        }

        public void RequestFriendsList()
        {
            FriendsList = null;
            FriendSystem.Instance.RefreshFriendsList();
        }

        public async void OnGetFriendsReceived(List<FriendBackendController.Friend> friendsList)
        {
            foreach (FriendBackendController.Friend friend in friendsList)
            {
                if (friend is null || friend.Presence is not FriendBackendController.FriendPresence presence) continue;

                string userId = presence.FriendLinkId;

                TaskCompletionSource<GetAccountInfoResult> completionSource = new();
                GetAccountInfoResult currentAccountInfo = PlayerExtensions.GetAccountInfo(userId, completionSource.SetResult);
                currentAccountInfo ??= await completionSource.Task;
            }

            FriendsList = friendsList;

            SetContent();
        }

        public override InfoContent GetContent()
        {
            LineBuilder lines = new();

            // FriendSystem.PlayerPrivacy is more readable to users
            FriendBackendController.PrivacyState localPlayerPrivacy = FriendBackendController.Instance.MyPrivacyState;

            lines.Add($"Privacy: {localPlayerPrivacy.GetName().Replace('_', ' ').ToTitleCase()}", new Widget_SnapSlider((int)localPlayerPrivacy, 0, 2, selection =>
            {
                localPlayerPrivacy = (FriendBackendController.PrivacyState)selection;
                FriendBackendController.Instance.lastPrivacyState = localPlayerPrivacy;
                FriendBackendController.Instance.SetPrivacyState(localPlayerPrivacy);
                SetContent();
            })
            {
                Colour = ColourPalette.CreatePalette(ColourPalette.Green.GetInitialColour(), ColourPalette.Red.GetInitialColour())
            });
            lines.Skip();

            if (FriendsList == null)
            {
                lines.BeginCentre().Append("Loading friends list...").EndAlign().AppendLine();
                return lines;
            }

            FriendBackendController.Friend[] friendsList = [.. FriendsList.Where(friend => friend is not null).OrderBy(friend => friend.Created)];

            if (friendsList.Length == 0)
            {
                lines.BeginCentre().Append("Friends list is empty.").EndAlign().AppendLine();

                lines.Skip().Add("\"Jump on in and make some friends!\"");

                return lines;
            }

            lines.BeginCentre().Append("Showing ").Append(friendsList.Length).Append(" friends:").EndAlign().AppendLine();

            foreach (FriendBackendController.Friend friend in friendsList)
            {
                if (friend.Presence is not FriendBackendController.FriendPresence presence) continue;

                string userId = presence.FriendLinkId;

                GetAccountInfoResult accountInfo = PlayerExtensions.GetAccountInfo(userId, null);

                if (accountInfo is null || accountInfo.AccountInfo is null || accountInfo.AccountInfo.TitleInfo is null)
                    continue;

                string userName = presence.UserName;
                string roomId = presence.RoomId;
                bool? isPublic = presence.IsPublic;
                string zoneName = presence.Zone;

                string playerName = ((string.IsNullOrEmpty(userName) || string.IsNullOrWhiteSpace(userName)) && accountInfo.AccountInfo.TitleInfo.DisplayName != null && accountInfo.AccountInfo.TitleInfo.DisplayName.Length > 4) ? accountInfo.AccountInfo.TitleInfo.DisplayName[0..^4].EnforcePlayerNameLength() : userName;

                bool isRoomPublic = isPublic.GetValueOrDefault(false);
                bool isOffline = string.IsNullOrEmpty(roomId) || roomId.Length == 0;
                bool inVirtualStump = !isOffline && (roomId.StartsWith(GorillaComputer.instance.VStumpRoomPrepend) || (zoneName != null && zoneName.ToLower().Contains(GTZone.customMaps.GetName().ToLower())));
                bool inZone = !string.IsNullOrEmpty(zoneName) && ZoneManagement.instance.activeZones.Exists(zone => zoneName.ToLower().Contains(zone.GetName().ToLower()));
                bool privacyConfiguration = (isRoomPublic ? Configuration.ShowPublic : Configuration.ShowPrivate).Value;
                string visibleRoomId = privacyConfiguration ? roomId : $"-{(isRoomPublic ? "PUBLIC" : "PRIVATE")}-";
                string playerStatus = isOffline ? "Offline" : (inVirtualStump ? $"{roomId} in Virtual Stump" : ((isRoomPublic && presence.Zone != null) ? $"{visibleRoomId} in {presence.Zone.ToUpper()}" : visibleRoomId));

                if (isOffline)
                {
                    // Only text is provided when the friend is offline, no symbols to infer or buttons to push here
                    lines.Append(playerName).Append(": ").Append(playerStatus).AppendLine();
                    continue;
                }

                if (roomId.Equals(NetworkSystem.Instance.RoomName))
                {
                    // Local Player is in the room with the friend, no functionality is neccesary here
                    lines.Append(playerName).Append(": ").Append(playerStatus).Add(new Widget_PushButton()
                    {
                        Colour = ColourPalette.Yellow,
                        Symbol = (Symbol)Symbols.LightBulb
                    });
                    continue;
                }

                if ((!isRoomPublic || inZone) && !inVirtualStump)
                {
                    // Local Player is allowed to join the friend by using the push button
                    lines.Append(playerName).Append(": ").Append(playerStatus).Add(new Widget_PushButton(JoinFriend, friend, inVirtualStump)
                    {
                        Colour = ColourPalette.Green,
                        Symbol = (Symbol)Symbols.GreenFlag
                    });
                    continue;
                }

                // Local Player isn't allowed to join the friend and therefore no functionality is given
                lines.Append(playerName).Append(": ").Append(playerStatus).Add(new Widget_PushButton()
                {
                    Colour = ColourPalette.Red,
                    Symbol = (Symbol)Symbols.RedFlag
                });
            }

            return lines;
        }

        public void JoinFriend(object[] args)
        {
            if (args.ElementAtOrDefault(0) is FriendBackendController.FriendPresence presence && args.ElementAtOrDefault(1) is bool isVirtualStump)
            {
                if (isVirtualStump && !GorillaComputer.instance.IsPlayerInVirtualStump())
                {
                    // The Virtual Stump connection is currently inaccessible, therefore the following code isn't used... for now 
                    // Not only does the game disallow it with it's friend system, but I'm also not completely proud of this as well

                    try
                    {
                        GameObject treeRoom = ZoneManagement.instance.allObjects.First(gameObject => gameObject.name == "TreeRoom");
                        VirtualStumpTeleporter teleporter = treeRoom.GetComponentInChildren<VirtualStumpTeleporter>(true);
                        StartCoroutine(CustomMapManager.TeleportToVirtualStump(teleporter, success =>
                        {
                            if (success) JoinFriend(args);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Logging.Fatal("Failed to bring the local player to Virtual Stump when joining friend");
                        Logging.Error(ex);
                    }

                    return;
                }

                if (isVirtualStump && GorillaComputer.instance.IsPlayerInVirtualStump() && NetworkSystem.Instance.InRoom)
                {
                    CustomMapManager.ReturnToVirtualStump();
                }

                GorillaComputer.instance.roomToJoin = presence.RoomId;
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(presence.RoomId, presence.IsPublic.GetValueOrDefault(false) ? JoinType.FriendStationPublic : JoinType.FriendStationPrivate);
            }
        }
    }
}