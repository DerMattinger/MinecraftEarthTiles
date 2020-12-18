$("#verticalScale").change(function() {
	verticalScaleWarning();
});

$("#horizontalScale").change(function() {
	calculatePrice();
});

$("#longitude").change(function() {
	calculatePrice();
});

$("#latitude").change(function() {
	calculatePrice();
});

function verticalScaleWarning(){
	var verticalScale = $("#verticalScale").val();
	if(verticalScale < 50){
		$("#verticalScaleInfo").text("Warning: might not be possible whit this vertical scale.");
	}else{
		$("#verticalScaleInfo").text("");
	}
}

function calculatePrice(){
	var finalPrice = 0;
	var finalTime = 0;
	var fileSize = 0;
	var latitude = $("#latitude").val();
	var longitude = $("#longitude").val();
	var horizontalScale = $("#horizontalScale").val();
	
	var mapSize = (latitude * longitude) / (horizontalScale * horizontalScale);
	
	finalPrice = mapSize * 0.02;
	finalTime = mapSize * 0.03;
	fileSize = mapSize * 0.016;
	
	if(horizontalScale <= 200){
		finalPrice = finalPrice * 1.2
		finalTime = finalTime * 1.2
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