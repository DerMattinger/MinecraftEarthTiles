$("#generate").click(function() {
	generateXMLCode();
});

$("#verticalScale").change(function() {
	verticalScaleWarning();
});

function verticalScaleWarning(){
	var verticalScale = $("#verticalScale").val();
	if(verticalScale < 50){
		$("#verticalScaleInfo").text("Warning: might not be possible whit this vertical scale.");
	}else{
		$("#verticalScaleInfo").text("");
	}
}

function generateXMLCode(){
	var horizontalScale = $("#horizontalScale").val();
	var verticalScale = $("#verticalScale").val();
	var mapFormat = $("#mapFormat").val();
	var rivers = $("#rivers").val();
	var streams = $("#streams").val();
	var borders = $("#borders").val();
	var urban = $("#urban").val();
	var ores = $("#ores").val();
	var netherite = $("#netherite").val();
	var endportals = $("#endportals").val();
	var mobspawner = $("#mobspawner").val();
	var animalspawner = $("#animalspawner").val();
	var highways = $("#highways").val();
	var streets = $("#streets").val();
	var smallstreets = $("#smallstreets").val();
	var farms = $("#farms").val();
	var meadows = $("#meadows").val();
	var quarrys = $("#quarrys").val();
	var volcanos = $("#volcanos").val();
	var shrubs = $("#shrubs").val();
	var randomcrops = $("#randomcrops").val();
	
	var output = "";
	output += "Scale=" + horizontalScale;
	output += "\r\n";
	output += "Vertical Scale=" + verticalScale;
	output += "\r\n";
	output += "Map-Format=" + mapFormat;
	output += "\r\n";
	output += "Rivers=" + rivers;
	output += "\r\n";
	output += "Streams=" + streams;
	output += "\r\n";
	output += "Borders=" + borders;
	output += "\r\n";
	output += "Urban=" + urban;
	output += "\r\n";
	output += "Ores=" + ores;
	output += "\r\n";
	output += "Netherite=" + netherite;
	output += "\r\n";
	output += "End-Portals=" + endportals;
	output += "\r\n";
	output += "Mob-Spawner=" + mobspawner;
	output += "\r\n";
	output += "Animal-Spawner=" + animalspawner;
	output += "\r\n";
	output += "Highways=" + highways;
	output += "\r\n";
	output += "Streets=" + streets;
	output += "\r\n";
	output += "Small streets=" + smallstreets;
	output += "\r\n";
	output += "Farms=" + farms;
	output += "\r\n";
	output += "Meadows=" + meadows;
	output += "\r\n";
	output += "Quarrys=" + quarrys;
	output += "\r\n";
	output += "Volcanos=" + volcanos;
	output += "\r\n";
	output += "Shrubs=" + shrubs;
	output += "\r\n";
	output += "Random Crops=" + randomcrops;
	output += "\r\n";
	
	$("#output").text(output);
	
	output = encodeURI(output);
	output = output.replaceAll(" ", "%20");
	output = output.replaceAll("\r", "%0D");
	output = output.replaceAll("\n", "%0A");
	output = output.replaceAll(".", "%2E");
	output = output.replaceAll("-", "%2D");
	output = output.replaceAll("+", "%2B");
	output = output.replaceAll("=", "%3D");
	
	$("#mail").attr("href", "mailto:maps@motfe.net?subject=Custom%20Map%20Request&body=Hello%20EarthTiles%20Creators%2C%0D%0A%0D%0AI%20would%20like%20to%20request%20a%20custom%20map%20using%20the%20following%20settings%3A%0D%0A%0D%0A"+output);
	
}