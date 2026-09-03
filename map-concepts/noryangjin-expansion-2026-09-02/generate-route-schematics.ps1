$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Output = Join-Path $Root "reference/routes"
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$ExistingRoadModules = 51
$TargetTotalRoadModules = 230
$TargetAddedRoadModules = $TargetTotalRoadModules - $ExistingRoadModules
$MeasuredTravelModules = 13.5
$MeasuredTravelSeconds = 17.5
$MeasuredSecondsPerModule = $MeasuredTravelSeconds / $MeasuredTravelModules
$ExpectedRouteCount = 30

$Routes = @(
    [pscustomobject]@{ Id="super-radical-01"; Group="super-radical"; Name="Black-Route Overpass Knot"; Spec="S24,E8,N19,W25,N18,W13,N10,W26,N20,W6,N10"; Concept="crosses above the preserved route and folds north" },
    [pscustomobject]@{ Id="super-radical-02"; Group="super-radical"; Name="Sacred-Altar Sky Braid"; Spec="S24,E6,N22,W13,N17,W7,N23,W22,N8,W11,N17,E9"; Concept="braids around the altar district before an east exit" },
    [pscustomobject]@{ Id="super-radical-03"; Group="super-radical"; Name="Auction Roof Clover"; Spec="S25,E19,N8,W14,N6,W9,N10,W21,N16,E7,N22,W22"; Concept="clover-shaped auction roofs linked by an overpass" },
    [pscustomobject]@{ Id="super-radical-04"; Group="super-radical"; Name="Triple Harbor Braid"; Spec="S24,E18,S9,E8,N11,W6,N13,W26,N22,W19,N23"; Concept="three harbor chambers tied by a central crossing" },
    [pscustomobject]@{ Id="super-radical-05"; Group="super-radical"; Name="Old Market Skyhook"; Spec="S25,W6,N17,E10,N20,W17,N10,W9,N10,E6,N6,W24,N7,W12"; Concept="returns above the old market before hooking west" },
    [pscustomobject]@{ Id="super-radical-06"; Group="super-radical"; Name="Twin Gantry Knot"; Spec="S26,E13,N7,W8,N7,W14,N20,W8,N6,W18,N14,W6,N6,E13,S7,E6"; Concept="threads two gantries and exits east" },
    [pscustomobject]@{ Id="super-radical-07"; Group="super-radical"; Name="Northbound Coil Stack"; Spec="S31,W6,N6,E17,N20,W6,N18,E6,S11,E9,N10,E9,N12,E18"; Concept="stacks open coils on a northbound spine" },
    [pscustomobject]@{ Id="super-radical-08"; Group="super-radical"; Name="Five-Gate Clover"; Spec="S25,W22,N15,W6,N12,E13,N6,E7,S10,E12,N18,E17,N6,E10"; Concept="crosses three preserved lanes and one new loop" },
    [pscustomobject]@{ Id="super-radical-09"; Group="super-radical"; Name="Cargo Mobius Frame"; Spec="S26,W19,N11,E6,N6,E8,N16,E24,S13,W6,N24,E20"; Concept="turns a cargo frame inside out before the east ramp" },
    [pscustomobject]@{ Id="super-radical-10"; Group="super-radical"; Name="Crane-Top Labyrinth"; Spec="S25,E16,N15,E9,N11,W6,N11,W14,S14,W10,N8,W10,N8,W6,N10,E6"; Concept="climbs into a dense crane-top labyrinth" },
    [pscustomobject]@{ Id="super-radical-11"; Group="super-radical"; Name="Figure-Eight Fishery"; Spec="S30,E18,S6,E6,S6,E14,S8,E16,S6,E18,S6,E12,S6,W7,N10,W10"; Concept="runs a broad figure eight across fishery basins" },
    [pscustomobject]@{ Id="super-radical-12"; Group="super-radical"; Name="Broken Double Spiral"; Spec="S32,W7,N10,W6,S6,E20,S6,E9,S15,E19,S15,E25,S9"; Concept="two broken spirals share crossing decks" },
    [pscustomobject]@{ Id="super-radical-13"; Group="super-radical"; Name="Undertow Sea Knot"; Spec="S24,E10,S6,W6,N10,W13,S6,W6,S7,W6,S14,W16,S22,W18,N8,W7"; Concept="tight sea knot pulled toward the west breakwater" },
    [pscustomobject]@{ Id="super-radical-14"; Group="super-radical"; Name="Pier Serpent Cross"; Spec="S27,E13,S16,E20,S11,E6,S13,E6,S6,E13,S6,W11,S6,E6,N19"; Concept="serpentine piers cross twice before rising north" },
    [pscustomobject]@{ Id="super-radical-15"; Group="super-radical"; Name="Elevator Bridge Weave"; Spec="S26,W6,N6,E13,S15,E6,S13,E8,S8,E12,S12,E8,S17,W6,N9,E14"; Concept="alternates lift bridges and descending decks" },
    [pscustomobject]@{ Id="super-radical-16"; Group="super-radical"; Name="Four-Basin Overpass"; Spec="S24,E11,S6,W6,N13,W9,N14,W17,N15,W13,N16,E6,N14,E15"; Concept="four nested basins use two overpasses and one old-route cross" },
    [pscustomobject]@{ Id="super-radical-17"; Group="super-radical"; Name="Container Cloverleaf"; Spec="S27,E25,N21,E8,S7,W24,N8,W13,N19,W13,N14"; Concept="large container cloverleaf with a north highway handoff" },
    [pscustomobject]@{ Id="super-radical-18"; Group="super-radical"; Name="Lighthouse Infinity"; Spec="S27,W17,N24,E26,N12,E9,N13,E10,S6,W17,N18"; Concept="infinity-shaped lighthouse route with three level changes" },
    [pscustomobject]@{ Id="super-radical-19"; Group="super-radical"; Name="Triple-Deck Scissors"; Spec="S25,E17,S13,W13,N23,W13,N26,W10,N11,W8,S20"; Concept="three decks cut across the harbor like open scissors" },
    [pscustomobject]@{ Id="super-radical-20"; Group="super-radical"; Name="Twin-Island Figure Eight"; Spec="S24,W6,N18,E17,N21,W6,S17,E24,N16,E6,N9,E15"; Concept="two island loops meet at a protected crossing" },
    [pscustomobject]@{ Id="super-radical-21"; Group="super-radical"; Name="Harbor Circuit Breaker"; Spec="S26,W14,S9,E12,N13,W6,S8,E19,S24,E22,S6,E20"; Concept="three self-crossings break a wide harbor circuit" },
    [pscustomobject]@{ Id="super-radical-22"; Group="super-radical"; Name="Eastbound Mobius Dock"; Spec="S30,E16,S6,E18,S10,E15,S6,W6,N10,E15,S8,W19,S14,E6"; Concept="mobius-like dock folds toward an east exit" },
    [pscustomobject]@{ Id="super-radical-23"; Group="super-radical"; Name="West Stormwall Knot"; Spec="S24,W20,S10,W6,S6,W15,N10,W6,S14,E10,N10,W18,S18,W12"; Concept="stormwall knot crosses itself three times" },
    [pscustomobject]@{ Id="super-radical-24"; Group="super-radical"; Name="Drydock Switchmaze"; Spec="S24,E6,N10,W17,S17,W10,S8,W11,S14,E6,N6,W11,S15,W7,N6,E11"; Concept="switchback maze with three protected overpasses" },
    [pscustomobject]@{ Id="super-radical-25"; Group="super-radical"; Name="Crane Yard Infinity"; Spec="S29,E18,N6,W12,N15,W10,N18,W18,N13,W6,S6,E14,S14"; Concept="crane yards form an asymmetric infinity path" },
    [pscustomobject]@{ Id="super-radical-26"; Group="super-radical"; Name="Tidegate Clover"; Spec="S29,W24,S6,E15,N22,E15,N6,W12,N17,E6,N7,E14,N6"; Concept="four-cross tidegate clover ending north" },
    [pscustomobject]@{ Id="super-radical-27"; Group="super-radical"; Name="Quad-Cross Flyover"; Spec="S28,E6,N6,W17,N6,E15,S17,E17,S17,E6,S16,E6,N6,E16"; Concept="four self-crossings on alternating flyover levels" },
    [pscustomobject]@{ Id="super-radical-28"; Group="super-radical"; Name="Impossible Wharf Braid"; Spec="S29,W6,S6,W11,S7,W6,S18,W18,N6,E6,S19,E7,N17,W23"; Concept="impossible-looking wharf braid with four crossings" },
    [pscustomobject]@{ Id="super-radical-29"; Group="super-radical"; Name="Five-Cross Sea Knot"; Spec="S24,W7,S12,W15,S6,E14,S6,W6,N17,W6,S18,W20,S9,E6,N13"; Concept="five-cross sea knot packed into one outer harbor" },
    [pscustomobject]@{ Id="super-radical-30"; Group="super-radical"; Name="Final Interchange Chaos"; Spec="S29,E6,N6,W15,N6,E13,S20,E6,S11,E14,N12,E6,N8,E6,S6,W15"; Concept="five-cross interchange chaos before the west highway" }
)

# Existing 51-module route, shifted so the current endpoint is (0, 0).
$Current = @(
    [pscustomobject]@{ X=-8.0; Z=-7.0 },
    [pscustomobject]@{ X=-12.0; Z=-7.0 },
    [pscustomobject]@{ X=-12.0; Z=2.0 },
    [pscustomobject]@{ X=6.0; Z=2.0 },
    [pscustomobject]@{ X=6.0; Z=8.0 },
    [pscustomobject]@{ X=0.0; Z=8.0 },
    [pscustomobject]@{ X=0.0; Z=0.0 }
)

function Get-ExtensionPoints([string]$Spec) {
    $points = New-Object System.Collections.Generic.List[object]
    $x = 0.0
    $z = 0.0
    $points.Add([pscustomobject]@{ X=$x; Z=$z })

    foreach ($leg in $Spec.Split(',')) {
        $dir = $leg.Substring(0, 1)
        $count = [int]$leg.Substring(1)
        switch ($dir) {
            "N" { $z += $count }
            "S" { $z -= $count }
            "E" { $x += $count }
            "W" { $x -= $count }
            default { throw "Unknown direction: $dir" }
        }
        $points.Add([pscustomobject]@{ X=$x; Z=$z })
    }
    return $points.ToArray()
}

function Get-HighwayPreviewPoints($Route, $Extension) {
    $lastLeg = $Route.Spec.Split(',')[-1]
    $direction = $lastLeg.Substring(0, 1)
    $start = $Extension[$Extension.Count - 1]
    $x = $start.X
    $z = $start.Z
    switch ($direction) {
        "N" { $z += 12 }
        "S" { $z -= 12 }
        "E" { $x += 12 }
        "W" { $x -= 12 }
    }
    return @(
        [pscustomobject]@{ X=$start.X; Z=$start.Z },
        [pscustomobject]@{ X=$x; Z=$z })
}

function Get-Segments($Points) {
    $segments = @()
    for ($i = 0; $i -lt $Points.Count - 1; $i++) {
        $segments += [pscustomobject]@{ A=$Points[$i]; B=$Points[$i + 1]; Index=$i }
    }
    return $segments
}

function Test-SegmentIntersection($One, $Two) {
    $oneVertical = $One.A.X -eq $One.B.X
    $twoVertical = $Two.A.X -eq $Two.B.X
    $oneMinX = [math]::Min($One.A.X, $One.B.X)
    $oneMaxX = [math]::Max($One.A.X, $One.B.X)
    $oneMinZ = [math]::Min($One.A.Z, $One.B.Z)
    $oneMaxZ = [math]::Max($One.A.Z, $One.B.Z)
    $twoMinX = [math]::Min($Two.A.X, $Two.B.X)
    $twoMaxX = [math]::Max($Two.A.X, $Two.B.X)
    $twoMinZ = [math]::Min($Two.A.Z, $Two.B.Z)
    $twoMaxZ = [math]::Max($Two.A.Z, $Two.B.Z)

    if ($oneVertical -and $twoVertical) {
        return $One.A.X -eq $Two.A.X -and [math]::Max($oneMinZ, $twoMinZ) -le [math]::Min($oneMaxZ, $twoMaxZ)
    }
    if (-not $oneVertical -and -not $twoVertical) {
        return $One.A.Z -eq $Two.A.Z -and [math]::Max($oneMinX, $twoMinX) -le [math]::Min($oneMaxX, $twoMaxX)
    }

    $vertical = if ($oneVertical) { $One } else { $Two }
    $horizontal = if ($oneVertical) { $Two } else { $One }
    $hMinX = [math]::Min($horizontal.A.X, $horizontal.B.X)
    $hMaxX = [math]::Max($horizontal.A.X, $horizontal.B.X)
    $vMinZ = [math]::Min($vertical.A.Z, $vertical.B.Z)
    $vMaxZ = [math]::Max($vertical.A.Z, $vertical.B.Z)
    return $vertical.A.X -ge $hMinX -and $vertical.A.X -le $hMaxX -and $horizontal.A.Z -ge $vMinZ -and $horizontal.A.Z -le $vMaxZ
}

function Get-SegmentContact($One, $Two) {
    if (-not (Test-SegmentIntersection $One $Two)) { return $null }

    $oneVertical = $One.A.X -eq $One.B.X
    $twoVertical = $Two.A.X -eq $Two.B.X
    if ($oneVertical -eq $twoVertical) {
        return [pscustomobject]@{ Kind='overlap'; X=0.0; Z=0.0 }
    }

    $vertical = if ($oneVertical) { $One } else { $Two }
    $horizontal = if ($oneVertical) { $Two } else { $One }
    $x = $vertical.A.X
    $z = $horizontal.A.Z
    $proper =
        $x -gt [math]::Min($horizontal.A.X, $horizontal.B.X) -and
        $x -lt [math]::Max($horizontal.A.X, $horizontal.B.X) -and
        $z -gt [math]::Min($vertical.A.Z, $vertical.B.Z) -and
        $z -lt [math]::Max($vertical.A.Z, $vertical.B.Z)

    return [pscustomobject]@{
        Kind = if ($proper) { 'crossing' } else { 'touch' }
        X = $x
        Z = $z
    }
}

function Get-RouteCrossings($Route, $Extension) {
    $extensionSegments = @(Get-Segments $Extension)
    $currentSegments = @(Get-Segments $Current)
    $crossings = @()
    $seenPoints = @{}

    for ($i = 0; $i -lt $extensionSegments.Count; $i++) {
        for ($j = $i + 2; $j -lt $extensionSegments.Count; $j++) {
            $contact = Get-SegmentContact $extensionSegments[$i] $extensionSegments[$j]
            if ($null -eq $contact) { continue }
            if ($contact.Kind -ne 'crossing') {
                throw "$($Route.Id): extension segments $i and $j overlap or touch instead of crossing"
            }

            $pointKey = "$($contact.X),$($contact.Z)"
            if ($seenPoints.ContainsKey($pointKey)) {
                throw "$($Route.Id): multiple crossings share $pointKey"
            }
            $seenPoints[$pointKey] = $true

            $crossings += [pscustomobject]@{
                Kind = 'self'
                LowerSegment = $i
                UpperSegment = $j
                X = $contact.X
                Z = $contact.Z
            }
        }
    }

    for ($i = 0; $i -lt $extensionSegments.Count; $i++) {
        for ($j = 0; $j -lt $currentSegments.Count; $j++) {
            $contact = Get-SegmentContact $extensionSegments[$i] $currentSegments[$j]
            if ($null -eq $contact) { continue }

            $allowedJunction = $i -eq 0 -and $j -eq ($currentSegments.Count - 1)
            if ($allowedJunction) { continue }
            if ($contact.Kind -ne 'crossing') {
                throw "$($Route.Id): extension segment $i overlaps or touches preserved segment $j"
            }

            $pointKey = "$($contact.X),$($contact.Z)"
            if ($seenPoints.ContainsKey($pointKey)) {
                throw "$($Route.Id): multiple crossings share $pointKey"
            }
            $seenPoints[$pointKey] = $true

            $crossings += [pscustomobject]@{
                Kind = 'preserved'
                LowerSegment = "current:$j"
                UpperSegment = $i
                X = $contact.X
                Z = $contact.Z
            }
        }
    }

    if ($crossings.Count -lt 2 -or $crossings.Count -gt 5) {
        throw "$($Route.Id): expected 2..5 intentional grade-separated crossings, found $($crossings.Count)"
    }

    return $crossings
}

function Assert-Route($Route, $Extension) {
    $legCounts = @($Route.Spec.Split(',') | ForEach-Object { [int]$_.Substring(1) })
    $moduleCount = ($legCounts | Measure-Object -Sum).Sum
    $totalModules = $ExistingRoadModules + $moduleCount
    if ($moduleCount -ne $TargetAddedRoadModules -or $totalModules -ne $TargetTotalRoadModules) {
        throw "$($Route.Id): expected $TargetAddedRoadModules new / $TargetTotalRoadModules total modules, found $moduleCount / $totalModules"
    }

    $minimumLeg = ($legCounts | Measure-Object -Minimum).Minimum
    $maximumLeg = ($legCounts | Measure-Object -Maximum).Maximum
    if ($Route.Group -ne 'super-radical' -or $minimumLeg -lt 6 -or $maximumLeg -gt 32) {
        throw "$($Route.Id): expected super-radical leg range 6..32, found $minimumLeg..$maximumLeg"
    }

    $firstLeg = $legCounts[0]
    if (-not $Route.Spec.StartsWith('S') -or $firstLeg -lt 24) {
        throw "$($Route.Id): opening must continue south for at least 24 modules"
    }
    if ($legCounts.Count -lt 11 -or $legCounts.Count -gt 16) {
        throw "$($Route.Id): leg count $($legCounts.Count) outside 11..16"
    }

    $metrics = Get-Metrics $Route
    $expectedSeconds = $TargetTotalRoadModules * $MeasuredSecondsPerModule
    if ([math]::Abs($metrics.Seconds - $expectedSeconds) -gt 0.001) {
        throw "$($Route.Id): measured-pace estimate $($metrics.Seconds) does not match $expectedSeconds"
    }

    Get-RouteCrossings $Route $Extension | Out-Null

    $extensionSegments = @(Get-Segments $Extension)
    $currentSegments = @(Get-Segments $Current)
    $highwayPreview = @(Get-HighwayPreviewPoints $Route $Extension)
    $highwaySegment = (Get-Segments $highwayPreview)[0]
    for ($i = 0; $i -lt $extensionSegments.Count - 1; $i++) {
        if (Test-SegmentIntersection $highwaySegment $extensionSegments[$i]) {
            throw "$($Route.Id): highway preview intersects extension segment $i"
        }
    }
    for ($i = 0; $i -lt $currentSegments.Count; $i++) {
        if (Test-SegmentIntersection $highwaySegment $currentSegments[$i]) {
            throw "$($Route.Id): highway preview intersects preserved segment $i"
        }
    }
}
function Get-Metrics($Route) {
    $modules = 0
    $turns = 0
    $previousDirection = "S"
    foreach ($leg in $Route.Spec.Split(',')) {
        $direction = $leg.Substring(0, 1)
        $modules += [int]$leg.Substring(1)
        if ($direction -ne $previousDirection) { $turns++ }
        $previousDirection = $direction
    }
    $totalTurns = 5 + $turns
    $totalModules = $ExistingRoadModules + $modules
    $seconds = $totalModules * $MeasuredSecondsPerModule
    return [pscustomobject]@{ AddedModules=$modules; TotalModules=$totalModules; AddedTurns=$turns; TotalTurns=$totalTurns; Seconds=$seconds }
}

function Get-ZoneAllocation($Route) {
    $legs = @($Route.Spec.Split(','))
    if ($legs.Count -lt 3) {
        throw "$($Route.Id): unsupported leg count $($legs.Count)"
    }

    [int]$outerZoneCount = [math]::Floor($legs.Count / 3)
    [int]$zone5Start = $legs.Count - $outerZoneCount
    $zone3 = @($legs[0..($outerZoneCount - 1)])
    $zone4 = @($legs[$outerZoneCount..($zone5Start - 1)])
    $zone5 = @($legs[$zone5Start..($legs.Count - 1)])

    return [pscustomobject]@{
        zone1And2 = "preserved current route"
        zone3 = "current entry + " + ($zone3 -join ',')
        zone4 = $zone4 -join ','
        zone5 = $zone5 -join ','
        startAnchor = "sacred altar + illegal shoe modifier at preserved stage start"
        highwayPreview = "short ramp at final endpoint"
    }
}

function Get-PointAtRouteDistance($Extension, [string]$Spec, [double]$Distance) {
    $legs = @($Spec.Split(','))
    $travelled = 0.0
    for ($i = 0; $i -lt $legs.Count; $i++) {
        $length = [double]$legs[$i].Substring(1)
        if ($Distance -le $travelled + $length) {
            $ratio = ($Distance - $travelled) / $length
            $start = $Extension[$i]
            $end = $Extension[$i + 1]
            return [pscustomobject]@{
                X = $start.X + ($end.X - $start.X) * $ratio
                Z = $start.Z + ($end.Z - $start.Z) * $ratio
            }
        }
        $travelled += $length
    }
    return $Extension[$Extension.Count - 1]
}

function Get-RoadProfile($Route, $Extension) {
    $legs = @($Route.Spec.Split(','))
    $lengths = @($legs | ForEach-Object { [int]$_.Substring(1) })
    $crossings = @(Get-RouteCrossings $Route $Extension)
    $features = @()
    $bridgeModules = 0
    $uphillModules = 0
    $downhillModules = 0
    $featureIndex = 0

    foreach ($crossingGroup in @($crossings | Group-Object UpperSegment | Sort-Object { [int]$_.Name })) {
        $segmentIndex = [int]$crossingGroup.Name
        $segmentStart = 0
        for ($i = 0; $i -lt $segmentIndex; $i++) { $segmentStart += $lengths[$i] }
        $segmentLength = $lengths[$segmentIndex]
        $crossingDistances = @()

        foreach ($crossing in $crossingGroup.Group) {
            $segmentPoint = $Extension[$segmentIndex]
            $offset =
                [math]::Abs($crossing.X - $segmentPoint.X) +
                [math]::Abs($crossing.Z - $segmentPoint.Z)
            $routeDistance = $segmentStart + $offset
            if ($routeDistance -lt $segmentStart + 4 -or $routeDistance -gt $segmentStart + $segmentLength - 4) {
                throw "$($Route.Id): crossing on segment $segmentIndex is too close to a turn"
            }
            $crossingDistances += $routeDistance
        }

        $startDistance = [math]::Max($segmentStart + 2, ($crossingDistances | Measure-Object -Minimum).Minimum - 3)
        $endDistance = [math]::Min($segmentStart + $segmentLength - 2, ($crossingDistances | Measure-Object -Maximum).Maximum + 3)
        $featureType = if ($featureIndex % 2 -eq 0) { 'bridge' } else { 'elevated' }
        $features += [pscustomobject]@{
            Type = $featureType
            StartDistance = $startDistance
            EndDistance = $endDistance
            StartPoint = Get-PointAtRouteDistance $Extension $Route.Spec $startDistance
            EndPoint = Get-PointAtRouteDistance $Extension $Route.Spec $endDistance
            CrossingCount = $crossingGroup.Count
            SegmentIndex = $segmentIndex
        }

        if ($featureType -eq 'bridge') {
            $bridgeModules += [int]($endDistance - $startDistance)
        }
        else {
            $uphillModules++
            $downhillModules++
        }
        $featureIndex++
    }

    foreach ($crossing in $crossings) {
        $segmentStart = 0
        for ($i = 0; $i -lt $crossing.UpperSegment; $i++) { $segmentStart += $lengths[$i] }
        $segmentPoint = $Extension[$crossing.UpperSegment]
        $offset =
            [math]::Abs($crossing.X - $segmentPoint.X) +
            [math]::Abs($crossing.Z - $segmentPoint.Z)
        $routeDistance = $segmentStart + $offset
        $cover = @($features | Where-Object {
            $_.SegmentIndex -eq $crossing.UpperSegment -and
            $_.StartDistance -le $routeDistance -and
            $_.EndDistance -ge $routeDistance
        })
        if ($cover.Count -ne 1) {
            throw "$($Route.Id): crossing at $($crossing.X),$($crossing.Z) is not protected by exactly one overpass"
        }
    }

    $leftTurns = 0
    $rightTurns = 0
    $directionOrder = @('N', 'E', 'S', 'W')
    $previousDirection = 'S'
    foreach ($leg in $legs) {
        $direction = $leg.Substring(0, 1)
        if ($direction -ne $previousDirection) {
            $previousIndex = [array]::IndexOf($directionOrder, $previousDirection)
            $nextIndex = [array]::IndexOf($directionOrder, $direction)
            $delta = ($nextIndex - $previousIndex + 4) % 4
            if ($delta -eq 1) { $rightTurns++ }
            elseif ($delta -eq 3) { $leftTurns++ }
            else { throw "$($Route.Id): non-90-degree direction transition" }
        }
        $previousDirection = $direction
    }

    $basicModules = $TargetAddedRoadModules - $bridgeModules - $uphillModules - $downhillModules - $leftTurns - $rightTurns
    if ($basicModules -lt 0) { throw "$($Route.Id): negative basic-road count" }

    return [pscustomobject]@{
        Basic = $basicModules
        Bridge = $bridgeModules
        Uphill = $uphillModules
        Downhill = $downhillModules
        LeftTurn = $leftTurns
        RightTurn = $rightTurns
        CrossingCount = $crossings.Count
        SelfCrossings = @($crossings | Where-Object Kind -eq 'self').Count
        PreservedCrossings = @($crossings | Where-Object Kind -eq 'preserved').Count
        Crossings = $crossings
        Features = $features
    }
}
function New-Pen([System.Drawing.Color]$Color, [float]$Width) {
    $pen = New-Object System.Drawing.Pen($Color, $Width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    return $pen
}

function New-RouteImage($Route, $Extension, $Metrics, $Profile) {
    $width = 4096
    $height = 4096
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(222, 239, 243))

    $highwayPreview = @(Get-HighwayPreviewPoints $Route $Extension)
    $allPoints = @($Current) + @($Extension) + @($highwayPreview)
    $minX = ($allPoints | Measure-Object X -Minimum).Minimum
    $maxX = ($allPoints | Measure-Object X -Maximum).Maximum
    $minZ = ($allPoints | Measure-Object Z -Minimum).Minimum
    $maxZ = ($allPoints | Measure-Object Z -Maximum).Maximum
    $plotLeft = 260.0
    $plotTop = 760.0
    $plotRight = 3836.0
    $plotBottom = 3750.0
    $spanX = [math]::Max(1.0, $maxX - $minX)
    $spanZ = [math]::Max(1.0, $maxZ - $minZ)
    $scale = [math]::Min(($plotRight - $plotLeft) / $spanX, ($plotBottom - $plotTop) / $spanZ)
    $usedWidth = $spanX * $scale
    $usedHeight = $spanZ * $scale
    $offsetX = $plotLeft + (($plotRight - $plotLeft) - $usedWidth) / 2.0
    $offsetY = $plotTop + (($plotBottom - $plotTop) - $usedHeight) / 2.0

    function Project($Point) {
        return [System.Drawing.PointF]::new(
            [float]($offsetX + ($Point.X - $minX) * $scale),
            [float]($offsetY + ($maxZ - $Point.Z) * $scale))
    }

    $gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(28, 69, 112, 124), 2)
    for ($x = $plotLeft; $x -le $plotRight; $x += [math]::Max(70, $scale * 4)) {
        $graphics.DrawLine($gridPen, [float]$x, [float]$plotTop, [float]$x, [float]$plotBottom)
    }
    for ($y = $plotTop; $y -le $plotBottom; $y += [math]::Max(70, $scale * 4)) {
        $graphics.DrawLine($gridPen, [float]$plotLeft, [float]$y, [float]$plotRight, [float]$y)
    }

    $roadWidth = [float][math]::Max(46, [math]::Min(105, $scale * 0.86))
    $land = New-Pen ([System.Drawing.Color]::FromArgb(249, 246, 229)) ($roadWidth + 48)
    $currentRoad = New-Pen ([System.Drawing.Color]::FromArgb(68, 78, 80)) $roadWidth
    $extensionColor = switch ($Route.Group) {
        "radical" { [System.Drawing.Color]::FromArgb(203, 63, 104) }
        "super-radical" { [System.Drawing.Color]::FromArgb(112, 54, 190) }
    }
    $extensionRoad = New-Pen $extensionColor $roadWidth
    $highwayLand = New-Pen ([System.Drawing.Color]::FromArgb(249, 246, 229)) ($roadWidth + 66)
    $highwayRoad = New-Pen ([System.Drawing.Color]::FromArgb(88, 96, 104)) ($roadWidth + 22)
    $currentCenter = New-Pen ([System.Drawing.Color]::FromArgb(247, 202, 75)) 7
    $currentCenter.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    $extensionCenter = New-Pen ([System.Drawing.Color]::FromArgb(235, 247, 255)) 7
    $extensionCenter.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    $highwayCenter = New-Pen ([System.Drawing.Color]::FromArgb(245, 245, 240)) 8
    $highwayCenter.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

    $currentProjected = @($Current | ForEach-Object { Project $_ })
    $extensionProjected = @($Extension | ForEach-Object { Project $_ })
    $highwayProjected = @($highwayPreview | ForEach-Object { Project $_ })
    $graphics.DrawLines($land, $currentProjected)
    $graphics.DrawLines($land, $extensionProjected)
    $graphics.DrawLines($highwayLand, $highwayProjected)
    $graphics.DrawLines($currentRoad, $currentProjected)
    $graphics.DrawLines($extensionRoad, $extensionProjected)
    $graphics.DrawLines($highwayRoad, $highwayProjected)

    $featureLand = New-Pen ([System.Drawing.Color]::FromArgb(249, 246, 229)) ($roadWidth + 24)
    $bridgePen = New-Pen ([System.Drawing.Color]::FromArgb(35, 190, 203)) ([float][math]::Max(24, $roadWidth - 18))
    $elevatedPen = New-Pen ([System.Drawing.Color]::FromArgb(247, 177, 52)) ([float][math]::Max(24, $roadWidth - 18))
    foreach ($feature in $Profile.Features) {
        $featureStart = Project $feature.StartPoint
        $featureEnd = Project $feature.EndPoint
        $graphics.DrawLine($featureLand, $featureStart, $featureEnd)
        $featurePen = if ($feature.Type -eq 'bridge') { $bridgePen } else { $elevatedPen }
        $graphics.DrawLine($featurePen, $featureStart, $featureEnd)
    }

    $graphics.DrawLines($currentCenter, $currentProjected)
    $graphics.DrawLines($extensionCenter, $extensionProjected)
    $graphics.DrawLines($highwayCenter, $highwayProjected)

    $turnBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 250, 249))
    $turnPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(31, 73, 83), 8)
    foreach ($point in $currentProjected[1..($currentProjected.Count - 2)]) {
        $graphics.FillEllipse($turnBrush, $point.X - 18, $point.Y - 18, 36, 36)
        $graphics.DrawEllipse($turnPen, $point.X - 18, $point.Y - 18, 36, 36)
    }
    foreach ($point in $extensionProjected[1..($extensionProjected.Count - 2)]) {
        $graphics.FillEllipse($turnBrush, $point.X - 18, $point.Y - 18, 36, 36)
        $graphics.DrawEllipse($turnPen, $point.X - 18, $point.Y - 18, 36, 36)
    }

    $crossingBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(249, 246, 229))
    $crossingPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(24, 43, 48), 7)
    foreach ($crossing in $Profile.Crossings) {
        $crossingPoint = Project ([pscustomobject]@{ X=$crossing.X; Z=$crossing.Z })
        $graphics.FillEllipse($crossingBrush, $crossingPoint.X - 15, $crossingPoint.Y - 15, 30, 30)
        $graphics.DrawEllipse($crossingPen, $crossingPoint.X - 15, $crossingPoint.Y - 15, 30, 30)
    }

    $startBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(32, 178, 118))
    $junctionBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 88, 55))
    $endBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(246, 169, 51))
    $start = $currentProjected[0]
    $junction = $extensionProjected[0]
    $end = $extensionProjected[$extensionProjected.Count - 1]
    $graphics.FillEllipse($startBrush, $start.X - 28, $start.Y - 28, 56, 56)
    $graphics.FillEllipse($junctionBrush, $junction.X - 30, $junction.Y - 30, 60, 60)
    $diamond = @(
        [System.Drawing.PointF]::new($end.X, $end.Y - 38),
        [System.Drawing.PointF]::new($end.X + 38, $end.Y),
        [System.Drawing.PointF]::new($end.X, $end.Y + 38),
        [System.Drawing.PointF]::new($end.X - 38, $end.Y)
    )
    $graphics.FillPolygon($endBrush, $diamond)

    $titleFont = New-Object System.Drawing.Font("Arial", 72, [System.Drawing.FontStyle]::Bold)
    $subtitleFont = New-Object System.Drawing.Font("Arial", 42, [System.Drawing.FontStyle]::Bold)
    $bodyFont = New-Object System.Drawing.Font("Arial", 31, [System.Drawing.FontStyle]::Regular)
    $legFont = New-Object System.Drawing.Font("Arial", 24, [System.Drawing.FontStyle]::Bold)
    $text = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(24, 43, 48))
    $muted = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(62, 87, 94))
    $graphics.DrawString("$($Route.Id.ToUpper())  |  $($Route.Name)", $titleFont, $text, 180, 90)
    $graphics.DrawString("CURRENT 51 + NEW $($Metrics.AddedModules) = $($Metrics.TotalModules) MODULES", $subtitleFont, $muted, 190, 200)
    $minutes = [math]::Floor($Metrics.Seconds / 60)
    $seconds = $Metrics.Seconds - $minutes * 60
    $graphics.DrawString(("{0}:{1:00.0} straight-run estimate  |  total turns {2}  |  measured 17.5 sec / 13.5 modules" -f $minutes, $seconds, $Metrics.TotalTurns), $subtitleFont, $muted, 190, 270)
    $graphics.DrawString("EXACT EXTENSION: $($Route.Spec)", $subtitleFont, $text, 190, 390)
    $graphics.DrawString("ROAD MIX: basic $($Profile.Basic)  |  bridge $($Profile.Bridge)  |  uphill $($Profile.Uphill)  |  downhill $($Profile.Downhill)  |  right $($Profile.RightTurn)  |  left $($Profile.LeftTurn)", $bodyFont, $text, 190, 485)
    $graphics.DrawString("purple = route   |   cyan = bridge-over   |   amber = raised-deck-over   |   ring = protected crossing   |   gray = Stage 2", $bodyFont, $muted, 190, 535)
    $graphics.DrawString("CROSSINGS: $($Profile.CrossingCount) total ($($Profile.SelfCrossings) self + $($Profile.PreservedCrossings) over current)   |   Experience: $($Route.Concept).", $bodyFont, $text, 190, 585)

    $highwayMidX = ($highwayProjected[0].X + $highwayProjected[1].X) / 2
    $highwayMidY = ($highwayProjected[0].Y + $highwayProjected[1].Y) / 2
    $highwayLabelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(225, 241, 244, 244))
    $graphics.FillRectangle($highwayLabelBrush, $highwayMidX - 112, $highwayMidY - 24, 224, 48)
    $graphics.DrawString("STAGE 2 HIGHWAY", $legFont, $text, $highwayMidX - 105, $highwayMidY - 21)

    $legs = $Route.Spec.Split(',')
    for ($i = 0; $i -lt $legs.Count; $i++) {
        $a = $extensionProjected[$i]
        $b = $extensionProjected[$i + 1]
        $midX = ($a.X + $b.X) / 2
        $midY = ($a.Y + $b.Y) / 2
        $labelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(225, 242, 247, 247))
        $graphics.FillRectangle($labelBrush, $midX - 46, $midY - 22, 92, 44)
        $graphics.DrawString($legs[$i], $legFont, $text, $midX - 39, $midY - 19)
        $labelBrush.Dispose()
    }

    $path = Join-Path $Output "$($Route.Id)-schematic-4096.png"
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    foreach ($item in @($gridPen, $land, $currentRoad, $extensionRoad, $highwayLand, $highwayRoad, $featureLand, $bridgePen, $elevatedPen, $currentCenter, $extensionCenter, $highwayCenter, $turnBrush, $turnPen, $crossingBrush, $crossingPen, $startBrush, $junctionBrush, $endBrush, $titleFont, $subtitleFont, $bodyFont, $legFont, $text, $muted, $highwayLabelBrush)) {
        $item.Dispose()
    }
    $graphics.Dispose()
    $bitmap.Dispose()
    return $path
}

if ($Routes.Count -ne $ExpectedRouteCount) {
    throw "Expected $ExpectedRouteCount routes, found $($Routes.Count)"
}
if (@($Routes | Where-Object Group -eq 'super-radical').Count -ne 30) {
    throw "Expected all 30 routes to be super-radical"
}

$seenShapes = @{}
foreach ($route in $Routes) {
    $directions = -join @($route.Spec.Split(',') | ForEach-Object { $_.Substring(0, 1) })
    $mirroredDirections = $directions.Replace('E', 'x').Replace('W', 'E').Replace('x', 'W')
    $shapeKey = (@($directions, $mirroredDirections) | Sort-Object)[0]
    if ($seenShapes.ContainsKey($shapeKey)) {
        throw "$($route.Id): duplicates or mirrors $($seenShapes[$shapeKey])"
    }
    $seenShapes[$shapeKey] = $route.Id
}

foreach ($route in $Routes) {
    $extension = @(Get-ExtensionPoints $route.Spec)
    Assert-Route $route $extension
    Get-RoadProfile $route $extension | Out-Null
}

$rootFullPath = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$outputFullPath = [System.IO.Path]::GetFullPath($Output).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $outputFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean route output outside concept root: $outputFullPath"
}
Get-ChildItem -LiteralPath $Output -Filter '*-schematic-4096.png' -File | Remove-Item -Force

$Manifest = @()
foreach ($route in $Routes) {
    $extension = @(Get-ExtensionPoints $route.Spec)
    Assert-Route $route $extension
    $metrics = Get-Metrics $route
    $profile = Get-RoadProfile $route $extension
    $zones = Get-ZoneAllocation $route
    $path = New-RouteImage $route $extension $metrics $profile
    $lastLeg = $route.Spec.Split(',')[-1]
    $Manifest += [pscustomobject]@{
        id = $route.Id
        group = $route.Group
        name = $route.Name
        spec = $route.Spec
        addedModules = $metrics.AddedModules
        totalModules = $metrics.TotalModules
        addedTurns = $metrics.AddedTurns
        totalTurns = $metrics.TotalTurns
        seconds = [math]::Round($metrics.Seconds, 4)
        timingBasis = "17.5 seconds per 13.5 road modules; turn pauses excluded"
        concept = $route.Concept
        highwayDirection = $lastLeg.Substring(0, 1)
        roadMix = [pscustomobject]@{
            basic = $profile.Basic
            bridge = $profile.Bridge
            uphill = $profile.Uphill
            downhill = $profile.Downhill
            rightTurn = $profile.RightTurn
            leftTurn = $profile.LeftTurn
        }
        crossings = [pscustomobject]@{
            total = $profile.CrossingCount
            self = $profile.SelfCrossings
            overCurrent = $profile.PreservedCrossings
            points = @($profile.Crossings | ForEach-Object {
                [pscustomobject]@{
                    kind = $_.Kind
                    x = $_.X
                    z = $_.Z
                    lowerSegment = $_.LowerSegment
                    upperSegment = $_.UpperSegment
                }
            })
        }
        features = @($profile.Features | ForEach-Object {
            [pscustomobject]@{
                type = $_.Type
                startModule = $_.StartDistance
                endModule = $_.EndDistance
                crossingCount = $_.CrossingCount
                segmentIndex = $_.SegmentIndex
            }
        })
        zones = $zones
        schematic = $path.Substring($Root.Length + 1).Replace('\', '/')
    }
}

$Manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Root "route-manifest.json") -Encoding UTF8
Write-Output "Generated and validated $($Manifest.Count) route schematics."
$Manifest | Format-Table id, addedModules, totalModules, totalTurns, seconds
