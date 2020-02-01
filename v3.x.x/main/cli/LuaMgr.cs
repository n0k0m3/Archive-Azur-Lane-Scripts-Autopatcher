using Azurlane.Properties;

namespace Azurlane
{
    internal static class LuaMgr
    {
        internal static int SuccessCount, FailedCount;

        internal static void Initialize(string lua, Tasks task)
        {
            var bytes = File.ReadAllBytes(lua);

            var state = State.None;
            if (bytes[3] == 0x80)
            {
                state = State.Encrypted;
                if (task == Tasks.Encrypt)
                {
                    Utils.LogInfo("{0} is already encrypted... <aborted>", true, true,
                        Path.GetFileName(lua).Replace(".txt", string.Empty));
                    return;
                }

                if (task == Tasks.Decompile)
                    Execute(lua, bytes, Tasks.Decrypt, state);
            }
            else if (bytes[3] == 0x02)
            {
                state = State.Decrypted;
                if (task == Tasks.Decrypt)
                {
                    Utils.LogInfo("{0} is already decrypted... <aborted>", true, true,
                        Path.GetFileName(lua).Replace(".txt", string.Empty));
                    return;
                }
            }
            else if (task != Tasks.Recompile)
            {
                Utils.LogInfo("Not a valid or damaged lua file... <aborted>", true, true);
                return;
            }

            if (task == Tasks.Decrypt || task == Tasks.Encrypt)
                Execute(lua, bytes, task, state);
            else if (task == Tasks.Decompile || task == Tasks.Recompile) Execute(lua, task);
            Program.IsValid = true;
        }

        private static void Execute(string lua, byte[] bytes, Tasks task, State state)
        {
            var luaPath = lua;

            if (Program.IsDevMode == false)
            {
                luaPath = Path.Combine(PathMgr.Local(task == Tasks.Decrypt ? "decrypted_Lua" : "encrypted_lua"),
                    Path.GetFileName(lua));

                if (File.Exists(luaPath))
                    File.Delete(luaPath);
            }

            try
            {
                Utils.LogInfo("{0} {1}...", true, false, task == Tasks.Decrypt ? "Decrypting" : "Encrypting",
                    Path.GetFileName(lua).Replace(".txt", string.Empty));
                using (var stream = new MemoryStream(bytes))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        // src: ljd\rawdump\header.py + Perfare
                        var magic = reader.ReadBytes(3);
                        var version = reader.ReadByte();
                        var bits = reader.ReadUleb128();

                        var is_stripped = (bits & 2u) != 0u;
                        if (!is_stripped)
                        {
                            var length = reader.ReadUleb128();
                            var name = Encoding.UTF8.GetString(reader.ReadBytes((int) length));
                        }

                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            var size = reader.ReadUleb128();

                            if (size == 0)
                                break;

                            var next = reader.BaseStream.Position + size;
                            bits = reader.ReadByte();

                            var arguments_count = reader.ReadByte();
                            var framesize = reader.ReadByte();
                            var upvalues_count = reader.ReadByte();
                            var complex_constants_count = reader.ReadUleb128();
                            var numeric_constants_count = reader.ReadUleb128();
                            var instructions_count = reader.ReadUleb128();

                            var start = (int) reader.BaseStream.Position;

                            if (state == State.Encrypted && task == Tasks.Decrypt)
                            {
                                bytes[3] = 0x02;
                                bytes = Unlock(start, bytes, (int) instructions_count);
                            }
                            else if (state == State.Decrypted && task == Tasks.Encrypt)
                            {
                                bytes[3] = 0x80;
                                bytes = Lock(start, bytes, (int) instructions_count);
                            }
                            else
                            {
                                break;
                            }

                            reader.BaseStream.Position = next;
                        }
                    }
                }

                File.WriteAllBytes(luaPath, bytes);
            }
            catch (Exception e)
            {
                Utils.LogException(
                    $"Exception detected (Executor.1) during {(task == Tasks.Decrypt ? "decrypting" : "encrypting")} {Path.GetFileName(lua).Replace(".txt", string.Empty)}",
                    e);
            }
            finally
            {
                if (File.Exists(luaPath))
                {
                    SuccessCount++;
                    Utils.Write(" <done>", false, true);
                }
                else
                {
                    FailedCount++;
                    Utils.Write(" <failed>", false, true);
                }
            }
        }

        private static void Execute(string lua, Tasks task)
        {
            var luaPath = lua;
            if (Program.IsDevMode == false)
            {
                lua = Path.Combine(PathMgr.Local(task == Tasks.Decompile ? "decrypted_lua" : "encrypted_lua"),
                    Path.GetFileName(lua));
                luaPath = Path.Combine(PathMgr.Local(task == Tasks.Decompile ? "decompiled_lua" : "recompiled_lua"),
                    Path.GetFileName(lua));

                if (File.Exists(luaPath))
                    File.Delete(luaPath);
            }

            Utils.LogInfo("{0} {1}...", true, false, task == Tasks.Decompile ? "Decompiling" : "Recompiling",
                Path.GetFileName(lua).Replace(".txt", string.Empty));
            try
            {
                Utils.Command(
                    task == Tasks.Decompile
                        ? $"python main.py -f \"{lua}\" -o \"{luaPath}\""
                        : $"luajit.exe -b \"{lua}\" \"{luaPath}\"",
                    PathMgr.Thirdparty(task == Tasks.Decompile ? "ljd" : "luajit"));
            }
            catch (Exception e)
            {
                Utils.LogException(
                    $"Exception detected (Executor.2) during {(task == Tasks.Decompile ? "decompiling" : "recompiling")} {Path.GetFileName(lua).Replace(".txt", string.Empty)}",
                    e);
            }
            finally
            {
                if (File.Exists(luaPath))
                {
                    SuccessCount++;
                    Utils.Write(" <done>", false, true);
                }
                else
                {
                    FailedCount++;
                    Utils.Write(" <failed>", false, true);
                }
            }
        }

        private static byte[] Lock(int start, byte[] bytes, int count)
        {
            var result = start;
            result += 4;
            var v2 = 0;
            do
            {
                var v3 = bytes[result - 4];
                result += 4;
                var v4 = bytes[result - 7] ^ v2++;
                bytes[result - 8] = (byte) (Resources.Lock[v3] ^ v4);
            } while (v2 != count);

            return bytes;
        }

        private static uint ReadUleb128(this BinaryReader reader)
        {
            // ljd\util\binstream.py + Perfare
            uint value = reader.ReadByte();
            if (value >= 0x80)
            {
                var bitshift = 0;
                value &= 0x7f;
                while (true)
                {
                    var b = reader.ReadByte();
                    bitshift += 7;
                    value |= (uint) ((b & 0x7f) << bitshift);
                    if (b < 0x80)
                        break;
                }
            }

            return value;
        }

        private static byte[] Unlock(int start, byte[] bytes, int count)
        {
            var result = start;
            result += 4;
            var v2 = 0;
            do
            {
                var v3 = bytes[result - 4];
                result += 4;
                var v4 = bytes[result - 7] ^ v3 ^ (v2++ & 0xFF);
                bytes[result - 8] = Resources.Unlock[v4];
            } while (v2 != count);

            return bytes;
        }

        internal enum State
        {
            None,
            Encrypted,
            Decrypted
        }
    }
}