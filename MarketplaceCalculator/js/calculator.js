$("#verticalScale").change(function() {
	verticalScaleWarning();
});

$("#horizontalScale").change(function() {
	calculateRoughPrice();
});

$("#longitude").change(function() {
	calculateRoughPrice();
});

$("#latitude").change(function() {
	calculateRoughPrice();
});

$("#blocks").change(function() {
	calculateCorrectPrice();
});

$("#tiles").change(function() {
	calculateCorrectPrice();
});

$("#amount").change(function() {
	calculateCorrectPrice();
});

function verticalScaleWarning(){
	var verticalScale = $("#verticalScale").val();
	if(verticalScale < 50){
		$("#verticalScaleInfo").text("Warning: might not be possible whit this vertical scale.");
	}else{
		$("#verticalScaleInfo").text("");
	}
}

function calculateCorrectPrice(){
	var mapPrice = 0;
	var finalPrice = 0;
	var finalTime = 0;
	var fileSize = 0;
	var blocks = $("#blocks").val();
	var tiles = $("#tiles").val();
	var amount = $("#amount").val();
	
	horizontalScale = round(36768000 / (blocks * (360 / tiles)));

	mapSize = 5.0 * (blocks / 512) * (blocks / 512) * amount / 20;
	
	mapPrice = mapSize * 0.01;
	finalTime = mapSize * 0.04;
	fileSize = mapSize * 0.02;
	
	$("#mapPrice").text(round2(mapPrice));
	
	if(horizontalScale <= 200){
		finalPrice = mapPrice * 1.2;
		finalTime = finalTime * 1.2;
		$("#scaleFactor").text(1.2);
	}else{
		finalPrice = mapPrice * 1;
		$("#scaleFactor").text(1);
	}
	
	finalPrice = finalPrice +5;
	
	if(finalPrice <= 10){
		finalPrice = 10;
		$("#info").text("The minimum price is 10 $.");
	}else{
		$("#info").text("");
	}
	
	$("#finalPrice").text(round2(finalPrice));
	$("#finalTime").text(round(finalTime));
	$("#fileSize").text(round2(fileSize));
}

function calculateRoughPrice(){
	var finalPrice = 0;
	var finalTime = 0;
	var fileSize = 0;
	var latitude = $("#latitude").val();
	var longitude = $("#longitude").val();
	var horizontalScale = $("#horizontalScale").val();
	
	var mapSize = (latitude * longitude) / (horizontalScale * horizontalScale);
	
	mapPrice = mapSize * 0.01;
	finalTime = mapSize * 0.04;
	fileSize = mapSize * 0.02;
	
	$("#mapPrice").text(round2(mapPrice));
	
	if(horizontalScale <= 200){
		finalPrice = mapPrice * 1.2;
		finalTime = finalTime * 1.2;
		$("#scaleFactor").text(1.2);
	}else{
		finalPrice = mapPrice * 1;
		$("#scaleFactor").text(1);
	}
	
	finalPrice = finalPrice +5;
	
	if(finalPrice <= 10){
		finalPrice = 10;
		$("#info").text("The minimum price is 10 $.");
	}else{
		$("#info").text("");
	}
	
	$("#finalPrice").text(round2(finalPrice));
	$("#finalTime").text(round(finalTime));
	$("#fileSize").text(round2(fileSize));
}

function round2(x) {
  return Number.parseFloat(x).toFixed(2);
}

function round(x) {
  return Number.parseFloat(x).toFixed(0);
}