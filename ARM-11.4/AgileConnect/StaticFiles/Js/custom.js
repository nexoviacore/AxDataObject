$(document).ready(function(){
	

$('#onofflineselect').selectpicker();

$('#smartwizard').smartWizard({
selected: 0,
theme: 'dots',
autoAdjustHeight:true,
transitionEffect:'fade',
showStepURLhash: false,
//enableAnchorOnDoneStep : true ,
});

$('.patient-onhold').on('click', function(e) {
  e.preventDefault();
  $('.modal-pop-onhold').toggleClass('is-visible');
});

$('.patient-review').on('click', function(e) {
  e.preventDefault();
  $('.modal-pop-review').toggleClass('is-visible');
});
$(".filter-option-inner-inner").append("<span class='onoffct-i'><i class='fa fa-angle-down' aria-hidden='true'></i></span>");

});

function senttoorderlab() {
  var checkBox = document.getElementById("tolab");
  var text = document.getElementById("sentlab");
  if (checkBox.checked == true){
    text.style.display = "block";
  } else {
     text.style.display = "none";
  }
}
function senttoorderpharmacy() {
  var checkBox = document.getElementById("topharmacy");
  var text = document.getElementById("sentpharmacy");
  if (checkBox.checked == true){
    text.style.display = "block";
  } else {
     text.style.display = "none";
  }
}