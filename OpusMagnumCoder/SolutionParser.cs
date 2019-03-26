using System.Collections.Generic;
using System.IO;

namespace OpusMagnumCoder
{
    public class SolutionParser
    {
        public Solution Parse(Stream stream)
        {
            using (var reader =
                new BinaryReader(stream))
            {
                if (reader.ReadInt32() != 7)
                {
                    throw new InvalidDataException();
                }

                var solution = new Solution {PuzzleName = reader.ReadString(), Name = reader.ReadString()};
                var n = reader.ReadInt32();
                int objectCount;
                if (n == 0)
                {
                    solution.Solved = false;
                    objectCount = reader.ReadInt32();
                }
                else
                {
                    solution.Solved = true;
                    reader.ReadInt32();
                    solution.Cycles = reader.ReadInt32();
                    reader.ReadInt32();
                    solution.Cost = reader.ReadInt32();
                    if (reader.ReadInt32() != 2) throw new InvalidDataException();

                    solution.Area = reader.ReadInt32();
                    if (reader.ReadInt32() != 3) throw new InvalidDataException();

                    solution.TotalSteps = reader.ReadInt32();
                    objectCount = reader.ReadInt32();
                }

                solution.Parts = new List<Part>();
                for (var i = 0; i < objectCount; i++)
                {
                    var part = new Part {Name = reader.ReadString()};
                    if (reader.ReadByte() != 1) throw new InvalidDataException();

                    part.Position = new Position(reader.ReadInt32(), reader.ReadInt32());
                    part.Size = reader.ReadInt32();
                    part.Rotation = reader.ReadInt32();
                    part.Index = reader.ReadInt32();
                    var stepCount = reader.ReadInt32();
                    part.Steps = new List<Step>();
                    for (var j = 0; j < stepCount; j++)
                        part.Steps.Add(new Step(reader.ReadInt32(), (Action) reader.ReadByte()));

                    part.TrackPositions = new List<Position>();
                    if (part.Name == "track")
                    {
                        var trackLength = reader.ReadInt32();
                        for (var j = 0; j < trackLength; j++)
                            part.TrackPositions.Add(new Position(reader.ReadInt32(), reader.ReadInt32()));
                    }

                    part.Number = reader.ReadInt32();

                    solution.Parts.Add(part);
                }

                return solution;
            }
        }

        public (Solution, Dictionary<string, string>) ParseWithCode(Stream stream)
        {
            using (var reader =
                new BinaryReader(stream))
            {
                if (reader.ReadInt32() != 19)
                {
                    throw new InvalidDataException();
                }

                var code = new Dictionary<string, string>();
                var codeCount = reader.ReadInt32();
                for (var i = 0; i < codeCount; i++) code.Add(reader.ReadString(), reader.ReadString());
                return (Parse(stream), code);

            }
        }

        public void Write(Solution solution, Stream stream)
        {
            using (var writer =
                new BinaryWriter(stream))
            {
                writer.Write(7);
                writer.Write(solution.PuzzleName);
                writer.Write(solution.Name);
                writer.Write(0);
                writer.Write(solution.Parts.Count);
                foreach (var part in solution.Parts)
                {
                    writer.Write(part.Name);
                    writer.Write((byte) 1);
                    writer.Write(part.Position.X);
                    writer.Write(part.Position.Y);
                    writer.Write(part.Size);
                    writer.Write(part.Rotation);
                    writer.Write(part.Index);
                    writer.Write(part.Steps.Count);
                    foreach (var step in part.Steps)
                    {
                        writer.Write(step.Index);
                        writer.Write((byte) step.Action);
                    }

                    if (part.Name == "track")
                    {
                        writer.Write(part.TrackPositions.Count);
                        foreach (var position in part.TrackPositions)
                        {
                            writer.Write(position.X);
                            writer.Write(position.Y);
                        }
                    }

                    writer.Write(part.Number);
                }
            }
        }

        public void WriteWithCode(Solution solution, ICollection<(string, string)> code, Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(19);
                writer.Write(code.Count);
                foreach (var (id, lines) in code)
                {
                    writer.Write(id);
                    writer.Write(lines);
                }

                Write(solution, stream);
            }
        }
    }
}