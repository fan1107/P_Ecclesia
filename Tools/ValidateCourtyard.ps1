# Run with PowerShell 7 (pwsh); its C# compiler supports the Unity source syntax.
$ErrorActionPreference = 'Stop'
$vector = @"
namespace UnityEngine {
 public struct Vector2Int {
  public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;}
  public static Vector2Int up=>new Vector2Int(0,1);
  public static Vector2Int down=>new Vector2Int(0,-1);
  public static Vector2Int left=>new Vector2Int(-1,0);
  public static Vector2Int right=>new Vector2Int(1,0);
  public static Vector2Int operator +(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x+b.x,a.y+b.y);
  public static bool operator ==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;
  public static bool operator !=(Vector2Int a,Vector2Int b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector2Int && this==(Vector2Int)o;
  public override int GetHashCode()=>x*397^y;
 }
}
"@
$gridSource = Get-Content -Raw -LiteralPath Assets/Ecclesia/Runtime/WalkGrid.cs
$layoutSource = Get-Content -Raw -LiteralPath Assets/Ecclesia/Runtime/CourtyardLayout.cs
Add-Type -TypeDefinition ($gridSource + $layoutSource + $vector)
$grid = New-Object Ecclesia.WalkGrid
[Ecclesia.CourtyardLayout]::Apply($grid)
$start = New-Object UnityEngine.Vector2Int(9,4)
$count = 0
for ($x=0; $x -lt 18; $x++) {
 for ($y=0; $y -lt 32; $y++) {
  $cell = New-Object UnityEngine.Vector2Int($x,$y)
  if (!$grid.IsOpen($cell)) { continue }
  $path = $grid.FindPath($start,$cell)
  if ($null -eq $path) { throw "Unreachable cell: $x,$y" }
  foreach ($step in $path) { if (!$grid.IsOpen($step)) { throw 'Path crosses obstacle' } }
  $count++
 }
}
foreach ($xy in @(@(3,8),@(14,8),@(6,17),@(12,17),@(9,19))) {
 if (!$grid.IsOpen((New-Object UnityEngine.Vector2Int($xy[0],$xy[1])))) { throw 'Expected lawn/door open' }
}
foreach ($xy in @(@(4,14),@(13,14),@(4,16),@(14,17),@(1,8),@(9,20))) {
 if ($grid.IsOpen((New-Object UnityEngine.Vector2Int($xy[0],$xy[1])))) { throw 'Expected obstacle closed' }
}
Write-Output "PASS: $count courtyard cells reachable; lawns and door open; walls and furniture blocked. Compiled actual WalkGrid/CourtyardLayout with managed Vector2Int test substitute."
