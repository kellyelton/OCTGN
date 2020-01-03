/*
 * This file was automatically generated!
 * Do not modify, changes will get lost when the file is regenerated!
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Octgn.Play;
using log4net;

namespace Octgn.Networking
{
	sealed class BinaryParser
	{
		private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private readonly Handler _handler;

		public BinaryParser(Handler handler)
		{ _handler = handler ?? throw new ArgumentNullException(nameof(handler)); }

		public void Parse(byte[] data)
		{
			var stream = new MemoryStream(data);
			var reader = new BinaryReader(stream);
			short length;
			_handler.SetMuted(reader.ReadInt32());
			var method = reader.ReadByte();
			switch (method)
			{
				case 0:
				{
					var arg0 = reader.ReadString();
					Log.Debug($"OCTGN IN: Error");
					_handler.Error(arg0);
					break;
				}
				case 2:
				{
					var arg0 = reader.ReadString();
					Log.Debug($"OCTGN IN: Kick");
					_handler.Kick(arg0);
					break;
				}
				case 5:
				{
					var arg0 = reader.ReadByte();
					var arg1 = new Guid(reader.ReadBytes(16));
					var arg2 = reader.ReadString();
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Welcome");
					_handler.Welcome(arg0, arg1, arg2, arg3);
					break;
				}
				case 6:
				{
					var arg0 = reader.ReadBoolean();
					var arg1 = reader.ReadBoolean();
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Settings");
					_handler.Settings(arg0, arg1, arg2);
					break;
				}
				case 7:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PlayerSettings] Player not found."); return; }
					var arg1 = reader.ReadBoolean();
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: PlayerSettings");
					_handler.PlayerSettings(arg0, arg1, arg2);
					break;
				}
				case 8:
				{
					var arg0 = reader.ReadByte();
					var arg1 = reader.ReadString();
					var arg2 = reader.ReadString();
					var arg3 = reader.ReadUInt64();
					var arg4 = reader.ReadBoolean();
					var arg5 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: NewPlayer");
					_handler.NewPlayer(arg0, arg1, arg2, arg3, arg4, arg5);
					break;
				}
				case 9:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Leave] Player not found."); return; }
					Log.Debug($"OCTGN IN: Leave");
					_handler.Leave(arg0);
					break;
				}
				case 10:
				{
					Log.Debug($"OCTGN IN: Start");
					_handler.Start();
					break;
				}
				case 12:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Reset] Player not found."); return; }
					Log.Debug($"OCTGN IN: Reset");
					_handler.Reset(arg0);
					break;
				}
				case 13:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[NextTurn] Player not found."); return; }
					var arg1 = reader.ReadBoolean();
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: NextTurn");
					_handler.NextTurn(arg0, arg1, arg2);
					break;
				}
				case 15:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[StopTurn] Player not found."); return; }
					Log.Debug($"OCTGN IN: StopTurn");
					_handler.StopTurn(arg0);
					break;
				}
				case 17:
				{
					var arg0 = reader.ReadByte();
					length = reader.ReadInt16();
					var arg1 = new Player[length];
					for (var i = 0; i < length; ++i)
					{
					  arg1[i] = Player.Find(_handler.GameEngine, reader.ReadByte());
					  if (arg1[i] == null)
					    Debug.WriteLine("[SetPhase] Player not found.");
					}
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: SetPhase");
					_handler.SetPhase(arg0, arg1, arg2);
					break;
				}
				case 19:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[SetActivePlayer] Player not found."); return; }
					Log.Debug($"OCTGN IN: SetActivePlayer");
					_handler.SetActivePlayer(arg0);
					break;
				}
				case 20:
				{
					Log.Debug($"OCTGN IN: ClearActivePlayer");
					_handler.ClearActivePlayer();
					break;
				}
				case 22:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Chat] Player not found."); return; }
					var arg1 = reader.ReadString();
					Log.Debug($"OCTGN IN: Chat");
					_handler.Chat(arg0, arg1);
					break;
				}
				case 24:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Print] Player not found."); return; }
					var arg1 = reader.ReadString();
					Log.Debug($"OCTGN IN: Print");
					_handler.Print(arg0, arg1);
					break;
				}
				case 26:
				{
					var arg0 = reader.ReadInt32();
					Log.Debug($"OCTGN IN: Random");
					_handler.Random(arg0);
					break;
				}
				case 28:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Counter] Player not found."); return; }
					var arg1 = Counter.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Counter] Counter not found."); return; }
					var arg2 = reader.ReadInt32();
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Counter");
					_handler.Counter(arg0, arg1, arg2, arg3);
					break;
				}
				case 29:
				{
					length = reader.ReadInt16();
					var arg0 = new int[length];
					for (var i = 0; i < length; ++i)
						arg0[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg1 = new Guid[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = new Guid(reader.ReadBytes(16));
					length = reader.ReadInt16();
					var arg2 = new Group[length];
					for (var i = 0; i < length; ++i)
					{
					  arg2[i] = Group.Find(_handler.GameEngine, reader.ReadInt32());
					  if (arg2[i] == null)
					    Debug.WriteLine("[LoadDeck] Group not found.");
					}
					length = reader.ReadInt16();
					var arg3 = new string[length];
					for (var i = 0; i < length; ++i)
						arg3[i] = reader.ReadString();
					var arg4 = reader.ReadString();
					var arg5 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: LoadDeck");
					_handler.LoadDeck(arg0, arg1, arg2, arg3, arg4, arg5);
					break;
				}
				case 30:
				{
					length = reader.ReadInt16();
					var arg0 = new int[length];
					for (var i = 0; i < length; ++i)
						arg0[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg1 = new Guid[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = new Guid(reader.ReadBytes(16));
					length = reader.ReadInt16();
					var arg2 = new string[length];
					for (var i = 0; i < length; ++i)
						arg2[i] = reader.ReadString();
					var arg3 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg3 == null)
					{ Debug.WriteLine("[CreateCard] Group not found."); return; }
					Log.Debug($"OCTGN IN: CreateCard");
					_handler.CreateCard(arg0, arg1, arg2, arg3);
					break;
				}
				case 31:
				{
					length = reader.ReadInt16();
					var arg0 = new int[length];
					for (var i = 0; i < length; ++i)
						arg0[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg1 = new Guid[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = new Guid(reader.ReadBytes(16));
					length = reader.ReadInt16();
					var arg2 = new int[length];
					for (var i = 0; i < length; ++i)
						arg2[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg3 = new int[length];
					for (var i = 0; i < length; ++i)
						arg3[i] = reader.ReadInt32();
					var arg4 = reader.ReadBoolean();
					var arg5 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: CreateCardAt");
					_handler.CreateCardAt(arg0, arg1, arg2, arg3, arg4, arg5);
					break;
				}
				case 32:
				{
					length = reader.ReadInt16();
					var arg0 = new int[length];
					for (var i = 0; i < length; ++i)
						arg0[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg1 = new ulong[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = reader.ReadUInt64();
					Log.Debug($"OCTGN IN: CreateAliasDeprecated");
					_handler.CreateAliasDeprecated(arg0, arg1);
					break;
				}
				case 34:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[MoveCard] Player not found."); return; }
					length = reader.ReadInt16();
					var arg1 = new int[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = reader.ReadInt32();
					var arg2 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[MoveCard] Group not found."); return; }
					length = reader.ReadInt16();
					var arg3 = new int[length];
					for (var i = 0; i < length; ++i)
						arg3[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg4 = new bool[length];
					for (var i = 0; i < length; ++i)
						arg4[i] = reader.ReadBoolean();
					var arg5 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: MoveCard");
					_handler.MoveCard(arg0, arg1, arg2, arg3, arg4, arg5);
					break;
				}
				case 36:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[MoveCardAt] Player not found."); return; }
					length = reader.ReadInt16();
					var arg1 = new int[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg2 = new int[length];
					for (var i = 0; i < length; ++i)
						arg2[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg3 = new int[length];
					for (var i = 0; i < length; ++i)
						arg3[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg4 = new int[length];
					for (var i = 0; i < length; ++i)
						arg4[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg5 = new bool[length];
					for (var i = 0; i < length; ++i)
						arg5[i] = reader.ReadBoolean();
					var arg6 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: MoveCardAt");
					_handler.MoveCardAt(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
					break;
				}
				case 38:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Peek] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Peek] Card not found."); return; }
					Log.Debug($"OCTGN IN: Peek");
					_handler.Peek(arg0, arg1);
					break;
				}
				case 40:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Untarget] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Untarget] Card not found."); return; }
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Untarget");
					_handler.Untarget(arg0, arg1, arg2);
					break;
				}
				case 42:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Target] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Target] Card not found."); return; }
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Target");
					_handler.Target(arg0, arg1, arg2);
					break;
				}
				case 44:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[TargetArrow] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[TargetArrow] Card not found."); return; }
					var arg2 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[TargetArrow] Card not found."); return; }
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: TargetArrow");
					_handler.TargetArrow(arg0, arg1, arg2, arg3);
					break;
				}
				case 45:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[Highlight] Card not found."); return; }
					var temp1 = reader.ReadString();
					var arg1 = temp1 == "" ? (Color?)null : (Color?)ColorConverter.ConvertFromString(temp1);
					Log.Debug($"OCTGN IN: Highlight");
					_handler.Highlight(arg0, arg1);
					break;
				}
				case 47:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Turn] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Turn] Card not found."); return; }
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: Turn");
					_handler.Turn(arg0, arg1, arg2);
					break;
				}
				case 49:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Rotate] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Rotate] Card not found."); return; }
					var arg2 = (CardOrientation)reader.ReadByte();
					Log.Debug($"OCTGN IN: Rotate");
					_handler.Rotate(arg0, arg1, arg2);
					break;
				}
				case 50:
				{
					var arg0 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[ShuffleDeprecated] Group not found."); return; }
					length = reader.ReadInt16();
					var arg1 = new int[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = reader.ReadInt32();
					Log.Debug($"OCTGN IN: ShuffleDeprecated");
					_handler.ShuffleDeprecated(arg0, arg1);
					break;
				}
				case 51:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Shuffled] Player not found."); return; }
					var arg1 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[Shuffled] Group not found."); return; }
					length = reader.ReadInt16();
					var arg2 = new int[length];
					for (var i = 0; i < length; ++i)
						arg2[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg3 = new short[length];
					for (var i = 0; i < length; ++i)
						arg3[i] = reader.ReadInt16();
					Log.Debug($"OCTGN IN: Shuffled");
					_handler.Shuffled(arg0, arg1, arg2, arg3);
					break;
				}
				case 52:
				{
					var arg0 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[UnaliasGrpDeprecated] Group not found."); return; }
					Log.Debug($"OCTGN IN: UnaliasGrpDeprecated");
					_handler.UnaliasGrpDeprecated(arg0);
					break;
				}
				case 53:
				{
					length = reader.ReadInt16();
					var arg0 = new int[length];
					for (var i = 0; i < length; ++i)
						arg0[i] = reader.ReadInt32();
					length = reader.ReadInt16();
					var arg1 = new ulong[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = reader.ReadUInt64();
					Log.Debug($"OCTGN IN: UnaliasDeprecated");
					_handler.UnaliasDeprecated(arg0, arg1);
					break;
				}
				case 55:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[AddMarker] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[AddMarker] Card not found."); return; }
					var arg2 = new Guid(reader.ReadBytes(16));
					var arg3 = reader.ReadString();
					var arg4 = reader.ReadUInt16();
					var arg5 = reader.ReadUInt16();
					var arg6 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: AddMarker");
					_handler.AddMarker(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
					break;
				}
				case 57:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[RemoveMarker] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[RemoveMarker] Card not found."); return; }
					var arg2 = new Guid(reader.ReadBytes(16));
					var arg3 = reader.ReadString();
					var arg4 = reader.ReadUInt16();
					var arg5 = reader.ReadUInt16();
					var arg6 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: RemoveMarker");
					_handler.RemoveMarker(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
					break;
				}
				case 59:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[TransferMarker] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[TransferMarker] Card not found."); return; }
					var arg2 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[TransferMarker] Card not found."); return; }
					var arg3 = new Guid(reader.ReadBytes(16));
					var arg4 = reader.ReadString();
					var arg5 = reader.ReadUInt16();
					var arg6 = reader.ReadUInt16();
					var arg7 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: TransferMarker");
					_handler.TransferMarker(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
					break;
				}
				case 61:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PassTo] Player not found."); return; }
					var arg1 = ControllableObject.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[PassTo] ControllableObject not found."); return; }
					var arg2 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg2 == null)
					{ Debug.WriteLine("[PassTo] Player not found."); return; }
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: PassTo");
					_handler.PassTo(arg0, arg1, arg2, arg3);
					break;
				}
				case 63:
				{
					var arg0 = ControllableObject.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[TakeFrom] ControllableObject not found."); return; }
					var arg1 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg1 == null)
					{ Debug.WriteLine("[TakeFrom] Player not found."); return; }
					Log.Debug($"OCTGN IN: TakeFrom");
					_handler.TakeFrom(arg0, arg1);
					break;
				}
				case 65:
				{
					var arg0 = ControllableObject.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[DontTake] ControllableObject not found."); return; }
					Log.Debug($"OCTGN IN: DontTake");
					_handler.DontTake(arg0);
					break;
				}
				case 66:
				{
					var arg0 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[FreezeCardsVisibility] Group not found."); return; }
					Log.Debug($"OCTGN IN: FreezeCardsVisibility");
					_handler.FreezeCardsVisibility(arg0);
					break;
				}
				case 68:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[GroupVis] Player not found."); return; }
					var arg1 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[GroupVis] Group not found."); return; }
					var arg2 = reader.ReadBoolean();
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: GroupVis");
					_handler.GroupVis(arg0, arg1, arg2, arg3);
					break;
				}
				case 70:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[GroupVisAdd] Player not found."); return; }
					var arg1 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[GroupVisAdd] Group not found."); return; }
					var arg2 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg2 == null)
					{ Debug.WriteLine("[GroupVisAdd] Player not found."); return; }
					Log.Debug($"OCTGN IN: GroupVisAdd");
					_handler.GroupVisAdd(arg0, arg1, arg2);
					break;
				}
				case 72:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[GroupVisRemove] Player not found."); return; }
					var arg1 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[GroupVisRemove] Group not found."); return; }
					var arg2 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg2 == null)
					{ Debug.WriteLine("[GroupVisRemove] Player not found."); return; }
					Log.Debug($"OCTGN IN: GroupVisRemove");
					_handler.GroupVisRemove(arg0, arg1, arg2);
					break;
				}
				case 74:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[LookAt] Player not found."); return; }
					var arg1 = reader.ReadInt32();
					var arg2 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[LookAt] Group not found."); return; }
					var arg3 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: LookAt");
					_handler.LookAt(arg0, arg1, arg2, arg3);
					break;
				}
				case 76:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[LookAtTop] Player not found."); return; }
					var arg1 = reader.ReadInt32();
					var arg2 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[LookAtTop] Group not found."); return; }
					var arg3 = reader.ReadInt32();
					var arg4 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: LookAtTop");
					_handler.LookAtTop(arg0, arg1, arg2, arg3, arg4);
					break;
				}
				case 78:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[LookAtBottom] Player not found."); return; }
					var arg1 = reader.ReadInt32();
					var arg2 = Group.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg2 == null)
					{ Debug.WriteLine("[LookAtBottom] Group not found."); return; }
					var arg3 = reader.ReadInt32();
					var arg4 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: LookAtBottom");
					_handler.LookAtBottom(arg0, arg1, arg2, arg3, arg4);
					break;
				}
				case 80:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[StartLimited] Player not found."); return; }
					length = reader.ReadInt16();
					var arg1 = new Guid[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = new Guid(reader.ReadBytes(16));
					Log.Debug($"OCTGN IN: StartLimited");
					_handler.StartLimited(arg0, arg1);
					break;
				}
				case 82:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[CancelLimited] Player not found."); return; }
					Log.Debug($"OCTGN IN: CancelLimited");
					_handler.CancelLimited(arg0);
					break;
				}
				case 83:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[CardSwitchTo] Player not found."); return; }
					var arg1 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg1 == null)
					{ Debug.WriteLine("[CardSwitchTo] Card not found."); return; }
					var arg2 = reader.ReadString();
					Log.Debug($"OCTGN IN: CardSwitchTo");
					_handler.CardSwitchTo(arg0, arg1, arg2);
					break;
				}
				case 84:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PlayerSetGlobalVariable] Player not found."); return; }
					var arg1 = reader.ReadString();
					var arg2 = reader.ReadString();
					var arg3 = reader.ReadString();
					Log.Debug($"OCTGN IN: PlayerSetGlobalVariable");
					_handler.PlayerSetGlobalVariable(arg0, arg1, arg2, arg3);
					break;
				}
				case 85:
				{
					var arg0 = reader.ReadString();
					var arg1 = reader.ReadString();
					var arg2 = reader.ReadString();
					Log.Debug($"OCTGN IN: SetGlobalVariable");
					_handler.SetGlobalVariable(arg0, arg1, arg2);
					break;
				}
				case 87:
				{
					_handler.Ping();
					break;
				}
				case 88:
				{
					var arg0 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: IsTableBackgroundFlipped");
					_handler.IsTableBackgroundFlipped(arg0);
					break;
				}
				case 89:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PlaySound] Player not found."); return; }
					var arg1 = reader.ReadString();
					Log.Debug($"OCTGN IN: PlaySound");
					_handler.PlaySound(arg0, arg1);
					break;
				}
				case 90:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[Ready] Player not found."); return; }
					Log.Debug($"OCTGN IN: Ready");
					_handler.Ready(arg0);
					break;
				}
				case 91:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PlayerState] Player not found."); return; }
					var arg1 = reader.ReadByte();
					Log.Debug($"OCTGN IN: PlayerState");
					_handler.PlayerState(arg0, arg1);
					break;
				}
				case 92:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[RemoteCall] Player not found."); return; }
					var arg1 = reader.ReadString();
					var arg2 = reader.ReadString();
					Log.Debug($"OCTGN IN: RemoteCall");
					_handler.RemoteCall(arg0, arg1, arg2);
					break;
				}
				case 93:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[GameStateReq] Player not found."); return; }
					Log.Debug($"OCTGN IN: GameStateReq");
					_handler.GameStateReq(arg0);
					break;
				}
				case 94:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[GameState] Player not found."); return; }
					var arg1 = reader.ReadString();
					Log.Debug($"OCTGN IN: GameState");
					_handler.GameState(arg0, arg1);
					break;
				}
				case 95:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[DeleteCard] Card not found."); return; }
					var arg1 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg1 == null)
					{ Debug.WriteLine("[DeleteCard] Player not found."); return; }
					Log.Debug($"OCTGN IN: DeleteCard");
					_handler.DeleteCard(arg0, arg1);
					break;
				}
				case 96:
				{
					var arg0 = Player.FindIncludingSpectators(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[PlayerDisconnect] Player not found."); return; }
					Log.Debug($"OCTGN IN: PlayerDisconnect");
					_handler.PlayerDisconnect(arg0);
					break;
				}
				case 98:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[AddPacks] Player not found."); return; }
					length = reader.ReadInt16();
					var arg1 = new Guid[length];
					for (var i = 0; i < length; ++i)
						arg1[i] = new Guid(reader.ReadBytes(16));
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: AddPacks");
					_handler.AddPacks(arg0, arg1, arg2);
					break;
				}
				case 99:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[AnchorCard] Card not found."); return; }
					var arg1 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg1 == null)
					{ Debug.WriteLine("[AnchorCard] Player not found."); return; }
					var arg2 = reader.ReadBoolean();
					Log.Debug($"OCTGN IN: AnchorCard");
					_handler.AnchorCard(arg0, arg1, arg2);
					break;
				}
				case 100:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[SetCardProperty] Card not found."); return; }
					var arg1 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg1 == null)
					{ Debug.WriteLine("[SetCardProperty] Player not found."); return; }
					var arg2 = reader.ReadString();
					var arg3 = reader.ReadString();
					var arg4 = reader.ReadString();
					Log.Debug($"OCTGN IN: SetCardProperty");
					_handler.SetCardProperty(arg0, arg1, arg2, arg3, arg4);
					break;
				}
				case 101:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[ResetCardProperties] Card not found."); return; }
					var arg1 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg1 == null)
					{ Debug.WriteLine("[ResetCardProperties] Player not found."); return; }
					Log.Debug($"OCTGN IN: ResetCardProperties");
					_handler.ResetCardProperties(arg0, arg1);
					break;
				}
				case 102:
				{
					var arg0 = Card.Find(_handler.GameEngine, reader.ReadInt32());
					if (arg0 == null)
					{ Debug.WriteLine("[Filter] Card not found."); return; }
					var temp1 = reader.ReadString();
					var arg1 = temp1 == "" ? (Color?)null : (Color?)ColorConverter.ConvertFromString(temp1);
					Log.Debug($"OCTGN IN: Filter");
					_handler.Filter(arg0, arg1);
					break;
				}
				case 103:
				{
					var arg0 = reader.ReadString();
					Log.Debug($"OCTGN IN: SetBoard");
					_handler.SetBoard(arg0);
					break;
				}
				case 104:
				{
					var arg0 = Player.Find(_handler.GameEngine, reader.ReadByte());
					if (arg0 == null)
					{ Debug.WriteLine("[SetPlayerColor] Player not found."); return; }
					var arg1 = reader.ReadString();
					Log.Debug($"OCTGN IN: SetPlayerColor");
					_handler.SetPlayerColor(arg0, arg1);
					break;
				}
		  default:
			  Debug.WriteLine("[Client Parser] Unknown message (id =" + method + ")");
				break;
			}
			reader.Close();
		}
	}
}
