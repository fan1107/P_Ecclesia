$ErrorActionPreference='Stop'
Add-Type -TypeDefinition (Get-Content -Raw Assets/Ecclesia/Runtime/PrayerArea.cs)
foreach($p in @(@(4.5,8.5),@(6.5,11.5),@(11.5,13.5),@(14.5,16.5),@(3.5,7.5),@(15.5,19.5))) {
 if(![Ecclesia.PrayerArea]::Contains($p[0],$p[1])) {throw "Near pew rejected: $p"}
}
foreach($p in @(@(9.5,4.5),@(9.5,14.5),@(5.5,22.5),@(1.5,10.5),@(5.5,5.5))) {
 if([Ecclesia.PrayerArea]::Contains($p[0],$p[1])) {throw "Distant location accepted: $p"}
}
'PASS: 6 pew/row-gap positions accepted; 5 entrance/aisle/altar/outside positions rejected.'
