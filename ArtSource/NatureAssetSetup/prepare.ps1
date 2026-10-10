$ErrorActionPreference = 'Stop'
$work = Join-Path $PWD 'ArtSource/NatureAssetSetup'
$inventory = Get-Content "$work/inventory-before.json" -Raw | ConvertFrom-Json
$plan = foreach ($item in $inventory) {
    $old = $item.path
    $base = [IO.Path]::GetFileNameWithoutExtension($old)
    $pack = $old.Split('/')[3]
    $category = ''
    $name = ''
    $solid = $false
    if ($pack -eq 'RocksGobi') {
        $n = [int]($base -replace '\D', '')
        $kind = switch ($n) { 2 {'RockSlab'} 3 {'RockSlab'} 10 {'RockSlab'} 4 {'RockWall'} 7 {'RockFormation'} 12 {'RockFormation'} 16 {'RockTall'} default {'Rock'} }
        $category = if ($kind -in @('RockWall','RockFormation','RockTall')) {'RockFormations'} elseif ($kind -eq 'RockSlab') {'RockSlabs'} else {'Rocks'}
        $name = '{0}_Gobi_{1:00}' -f $kind,$n
        $solid = $true
    } elseif ($pack -eq 'Lands' -and $base -match '^Peak(\d+)(A?)$') {
        $name = 'Rock_Lands_{0:00}' -f [int]$Matches[1]
        if ($Matches[2]) { $name += '_VariantA' }
        $category = 'Rocks'
        $solid = $true
    } elseif ($pack -eq 'RockyDesert') {
        switch -Regex ($base) {
            '^SM_RockSide_A1$' { $name='RockWall_Desert_01'; $category='RockWalls'; $solid=$true }
            '^SM_Rock_Side-A2$' { $name='RockWall_Desert_02'; $category='RockWalls'; $solid=$true }
            '^SM_Small_Rock_([1-4])$' { $name='Rock_Desert_{0:00}' -f [int]$Matches[1]; $category='Rocks'; $solid=$true }
            '^SM_Small_Rock_B([12])$' { $name='Pebble_Desert_{0:00}' -f [int]$Matches[1]; $category='Pebbles' }
            '^SM_DeadGrass_A(\d?)$' { $n=1; if($Matches[1]){$n=[int]$Matches[1]+1}; $name='GrassDry_Desert_{0:00}' -f $n; $category='Grass' }
            '^SM_Tree_1$' { $name='Tree_Desert_01'; $category='Trees' }
            '^SM_Tree_A$' { $name='Tree_Desert_02'; $category='Trees' }
            '^SM_Tree_B$' { $name='Tree_Desert_03'; $category='Trees' }
        }
    } elseif ($pack -eq 'URP_Tree_Pack') {
        $n=[int]($base -replace '\D',''); $name='Tree_TreePack_{0:00}' -f $n; $category='Trees'
    } elseif ($pack -eq 'Lands') {
        if ($base -match '^Tree(\d+)alien(?:\.prefabROTATE(\w+))?$') {
            $name='Tree_Lands_Alien_{0:00}' -f [int]$Matches[1]
            if($Matches[2]){$name+='_Rot'+$Matches[2]}
            $category='TreesAlien'
        } elseif ($base -match '^TreeP(\d+)$') {
            $name='Tree_Lands_SeriesP_{0:00}' -f [int]$Matches[1]; $category='TreesSeriesP'
        } elseif ($base -match '^Tree(\d+)$') {
            $name='Tree_Lands_{0:00}' -f [int]$Matches[1]; $category='Trees'
        } else {
            $plantNames=@{bamboo='Bamboo';broom='Broom';Fern='Fern_01';Fern2='Fern_02';Palm3='Palm_03';palm4='Palm_04';Palm5='Palm_05';palm6='Palm_06';rush='Rush';sunflower='Sunflower';wheat='Wheat'}
            $name='Plant_Lands_'+$plantNames[$base]; $category='Plants'
        }
    }
    if (!$name -or !$category) { throw "Unmapped prefab $old" }
    $new = "Assets/Art Resources/ThirdPartyPacks/$pack/Prefabs/$category/$name.prefab"
    if (Test-Path -LiteralPath $new) { throw "Destination exists $new" }
    $backup = Join-Path "$work/Before" $old
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($backup)) -Force | Out-Null
    Copy-Item -LiteralPath $old -Destination $backup
    Copy-Item -LiteralPath ($old+'.meta') -Destination ($backup+'.meta')
    [pscustomobject]@{oldPath=$old;newPath=$new;guid=$item.guid;name=$name;pack=$pack;category=$category;solid=$solid;hash=(Get-FileHash -LiteralPath $old -Algorithm SHA256).Hash}
}
if (($plan.newPath | Sort-Object -Unique).Count -ne $plan.Count) { throw 'Duplicate names' }
$plan | ConvertTo-Json -Depth 5 | Set-Content "$work/rename-plan.json" -Encoding utf8
$scenes = Get-ChildItem Assets/Scenes,Assets/SmokePortalTest -Recurse -Filter *.unity | ForEach-Object { [pscustomobject]@{path=$_.FullName;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} }
$scenes | ConvertTo-Json | Set-Content "$work/scenes-before.json" -Encoding utf8
$plan | Group-Object pack,category | Select-Object Name,Count | Format-Table -AutoSize
