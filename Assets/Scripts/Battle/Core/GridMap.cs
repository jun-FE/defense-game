using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Akmong.Battle
{
    /// <summary>칸 좌표(왼쪽 위가 0,0).</summary>
    public struct GridPoint
    {
        public int X, Y;
        public GridPoint(int x, int y) { X = x; Y = y; }
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>칸 하나의 종류. 결정은 크기와 상관없이 1×1칸이고 그림만 다르다.</summary>
    public enum CellType
    {
        /// <summary>암흑(탐색에서 밝힐 수 있음). 밝히기 전에는 벽이고 타워 설치 자리가 될 수 있다.</summary>
        Dark = 0,
        /// <summary>밝힌 바닥(길). 맵 제작 때 미리 열어 둔 길도 여기에 해당.</summary>
        Open = 1,
        /// <summary>밝힐 수 없는 암흑(막힌 벽).</summary>
        Wall = 2,
        /// <summary>작은 몽결정(밝히면 획득하고 그 칸은 길이 된다).</summary>
        CrystalSmall = 3,
        /// <summary>큰 몽결정.</summary>
        CrystalLarge = 4,
    }

    /// <summary>
    /// 탐색형 디펜스의 칸 맵(게임플레이 기획 요약). 좌표는 (x, y), 왼쪽 위가 (0, 0), 상하좌우로만 연결된다.
    /// 몬스터는 출현 지점(Spawn)에서 목표(Goal)까지 밝혀진 칸만 지나 최단 경로로 이동한다.
    /// 같은 거리의 경로가 여럿이면 이웃을 위 → 오른쪽 → 아래 → 왼쪽 순서로 살펴 항상 같은 경로를 고른다.
    /// 파일로 저장할 때는 줄마다 글자 하나가 칸 하나인 문자열(Rows)로 바꾼다: . 암흑, o 길, # 막힌 암흑, c 작은 결정, C 큰 결정.
    /// </summary>
    public sealed class GridMap
    {
        public const int MinSize = 6;
        public const int MaxSize = 96;

        /// <summary>위 → 오른쪽 → 아래 → 왼쪽(같은 거리일 때 고르는 순서).</summary>
        static readonly int[] DirX = { 0, 1, 0, -1 };
        static readonly int[] DirY = { -1, 0, 1, 0 };

        public string Id = "MAP_NEW";
        public string Name = "";
        public int Width { get; private set; }
        public int Height { get; private set; }
        /// <summary>몬스터 출현 지점. 없으면 -1.</summary>
        public int SpawnX = -1, SpawnY = -1;
        /// <summary>목표(꿈의 중심). 탐색은 여기서 시작한다. 없으면 -1.</summary>
        public int GoalX = -1, GoalY = -1;
        public int SmallCrystalValue = 10;
        public int LargeCrystalValue = 40;

        CellType[] cells;

        public GridMap(int width, int height)
        {
            Width = Clamp(width);
            Height = Clamp(height);
            cells = new CellType[Width * Height];
        }

        static int Clamp(int size) => Math.Max(MinSize, Math.Min(MaxSize, size));

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public CellType Get(int x, int y) => InBounds(x, y) ? cells[y * Width + x] : CellType.Wall;

        public void Set(int x, int y, CellType type)
        {
            if (InBounds(x, y)) cells[y * Width + x] = type;
        }

        public bool HasSpawn => InBounds(SpawnX, SpawnY);
        public bool HasGoal => InBounds(GoalX, GoalY);
        public bool IsSpawn(int x, int y) => x == SpawnX && y == SpawnY;
        public bool IsGoal(int x, int y) => x == GoalX && y == GoalY;

        /// <summary>지금 몬스터가 지나갈 수 있는 칸(밝힌 바닥, 출현 지점, 목표).</summary>
        public bool IsOpen(int x, int y) => InBounds(x, y) && (Get(x, y) == CellType.Open || IsSpawn(x, y) || IsGoal(x, y));

        /// <summary>탐색으로 언젠가 밝힐 수 있는 칸(막힌 암흑만 아니면 됨).</summary>
        public bool IsOpenable(int x, int y) => InBounds(x, y) && Get(x, y) != CellType.Wall;

        /// <summary>타워를 지을 수 있는 칸: 밝히지 않은 칸(암흑·막힌 암흑) 중 밝힌 바닥과 상하좌우로 맞닿은 벽 가장자리.</summary>
        public bool IsBuildable(int x, int y)
        {
            if (!InBounds(x, y) || IsOpen(x, y)) return false;
            CellType type = Get(x, y);
            if (type == CellType.CrystalSmall || type == CellType.CrystalLarge) return false;
            for (int d = 0; d < 4; d++)
                if (IsOpen(x + DirX[d], y + DirY[d])) return true;
            return false;
        }

        public int CrystalValue(CellType type) =>
            type == CellType.CrystalSmall ? SmallCrystalValue : type == CellType.CrystalLarge ? LargeCrystalValue : 0;

        public int Count(CellType type)
        {
            int n = 0;
            foreach (CellType c in cells) if (c == type) n++;
            return n;
        }

        /// <summary>맵 크기를 바꾼다. 왼쪽 위를 기준으로 겹치는 칸은 그대로 두고, 밖으로 나간 지점은 지운다.</summary>
        public void Resize(int width, int height)
        {
            int w = Clamp(width), h = Clamp(height);
            var next = new CellType[w * h];
            for (int y = 0; y < Math.Min(h, Height); y++)
                for (int x = 0; x < Math.Min(w, Width); x++)
                    next[y * w + x] = cells[y * Width + x];
            cells = next;
            Width = w;
            Height = h;
            if (!HasSpawn) SpawnX = SpawnY = -1;
            if (!HasGoal) GoalX = GoalY = -1;
        }

        public GridMap Clone()
        {
            var copy = (GridMap)MemberwiseClone();
            copy.cells = (CellType[])cells.Clone();
            return copy;
        }

        // ───────── 경로 ─────────

        /// <summary>지금 밝혀진 칸만으로 출현 지점 → 목표 최단 경로(칸 목록). 없으면 null.</summary>
        public List<GridPoint> ShortestPath() => ShortestPath(IsOpen);

        /// <summary>막힌 암흑을 뺀 모든 칸을 밝혔다고 칠 때의 최단 경로(맵 검사용). 없으면 null.</summary>
        public List<GridPoint> ShortestPathIfAllOpened() => ShortestPath(IsOpenable);

        /// <summary>너비 우선 탐색. 이웃 순서(위·오른쪽·아래·왼쪽)가 고정이라 같은 맵이면 항상 같은 경로가 나온다.</summary>
        public List<GridPoint> ShortestPath(Func<int, int, bool> passable)
        {
            if (!HasSpawn || !HasGoal) return null;
            var previous = new int[Width * Height];
            for (int i = 0; i < previous.Length; i++) previous[i] = -2;
            int start = SpawnY * Width + SpawnX, goal = GoalY * Width + GoalX;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            previous[start] = -1;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == goal) break;
                int cx = current % Width, cy = current / Width;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + DirX[d], ny = cy + DirY[d];
                    if (!InBounds(nx, ny)) continue;
                    int next = ny * Width + nx;
                    if (previous[next] != -2) continue;
                    bool endpoint = next == goal || next == start;
                    if (!endpoint && !passable(nx, ny)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (previous[goal] == -2) return null;
            var path = new List<GridPoint>();
            for (int at = goal; at != -1; at = previous[at]) path.Add(new GridPoint(at % Width, at / Width));
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 칸 경로를 전투 코어의 경로 점으로 바꾼다. 월드 좌표는 칸 중심, y는 위로 갈수록 커진다(worldY = Height - 1 - y).
        /// 꺾이는 칸만 남겨 점 수를 줄인다.
        /// </summary>
        public List<Vector2> ToWorldPath(List<GridPoint> cellsPath)
        {
            var points = new List<Vector2>();
            if (cellsPath == null) return points;
            for (int i = 0; i < cellsPath.Count; i++)
            {
                bool corner = i == 0 || i == cellsPath.Count - 1
                              || cellsPath[i - 1].X - cellsPath[i].X != cellsPath[i].X - cellsPath[i + 1].X
                              || cellsPath[i - 1].Y - cellsPath[i].Y != cellsPath[i].Y - cellsPath[i + 1].Y;
                if (corner) points.Add(new Vector2(cellsPath[i].X, Height - 1 - cellsPath[i].Y));
            }
            return points;
        }

        // ───────── 검사 ─────────

        /// <summary>맵 제작 검사. 오류 문장 목록(비어 있으면 통과).</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(Id)) errors.Add("맵 ID가 비어 있습니다.");
            if (!HasSpawn) errors.Add("시작점(몬스터 출현 지점)이 없습니다.");
            if (!HasGoal) errors.Add("끝점(목표)이 없습니다.");
            if (HasSpawn && HasGoal)
            {
                if (SpawnX == GoalX && SpawnY == GoalY) errors.Add("시작점과 끝점이 같은 칸입니다.");
                if (Get(SpawnX, SpawnY) == CellType.Wall) errors.Add("시작점이 막힌 암흑 위에 있습니다.");
                if (Get(GoalX, GoalY) == CellType.Wall) errors.Add("끝점이 막힌 암흑 위에 있습니다.");
                if (ShortestPathIfAllOpened() == null) errors.Add("막힌 암흑 때문에 시작점과 끝점을 이을 수 없습니다.");
            }
            if (SmallCrystalValue < 0 || LargeCrystalValue < 0) errors.Add("결정 값은 0 이상이어야 합니다.");
            return errors;
        }

        // ───────── 저장 형식 ─────────

        public string[] ToRows()
        {
            var rows = new string[Height];
            var sb = new StringBuilder(Width);
            for (int y = 0; y < Height; y++)
            {
                sb.Clear();
                for (int x = 0; x < Width; x++) sb.Append(ToChar(Get(x, y)));
                rows[y] = sb.ToString();
            }
            return rows;
        }

        /// <summary>줄 문자열로 맵을 만든다. 줄 길이가 다르면 가장 긴 줄에 맞추고 빈 곳은 암흑.</summary>
        public static GridMap FromRows(string[] rows)
        {
            int height = rows == null ? 0 : rows.Length;
            int width = 0;
            if (rows != null) foreach (string row in rows) width = Math.Max(width, row == null ? 0 : row.Length);
            var map = new GridMap(width, height);
            for (int y = 0; y < height && y < map.Height; y++)
                for (int x = 0; x < rows[y].Length && x < map.Width; x++)
                    map.Set(x, y, FromChar(rows[y][x]));
            return map;
        }

        public static char ToChar(CellType type)
        {
            switch (type)
            {
                case CellType.Open: return 'o';
                case CellType.Wall: return '#';
                case CellType.CrystalSmall: return 'c';
                case CellType.CrystalLarge: return 'C';
                default: return '.';
            }
        }

        public static CellType FromChar(char c)
        {
            switch (c)
            {
                case 'o': return CellType.Open;
                case '#': return CellType.Wall;
                case 'c': return CellType.CrystalSmall;
                case 'C': return CellType.CrystalLarge;
                default: return CellType.Dark;
            }
        }
    }
}
