$(document).ready(function(){
	

$('#onofflineselect').selectpicker();
$(".filter-option-inner-inner").append("<span class='onoffct-i'><i class='fa fa-angle-down' aria-hidden='true'></i></span>"); });

$('#smartwizard').smartWizard({
selected: 0,
theme: 'dots',
autoAdjustHeight:true,
transitionEffect:'fade',
showStepURLhash: false,
  anchorSettings: {
      anchorClickable: true, 
      enableAllAnchors: true,
  },
  keyboardSettings: {
      keyNavigation: true, 
      keyLeft: [37], 
      keyRight: [39]
  },
});


$('.patient-onhold').on('click', function(e) {
  e.preventDefault();
  $('.modal-pop-onhold').toggleClass('is-visible');
});
$('.patient-review').on('click', function(e) {
  e.preventDefault();
  $('.modal-pop-review').toggleClass('is-visible');
});



 
$(function () {
	     $("#sentlab").hide();
        $("#tolab").click(function () {
            if ($(this).is(":checked")) {
                $("#sentlab").show();
                $("#notsentlab").hide();
            } else {
                $("#sentlab").hide();
                $("#notsentlab").show();
            }
         });
 });
$(function () {
	    $("#sentpharmacy").hide();
        $("#topharmacy").click(function () {
            if ($(this).is(":checked")) {
                $("#sentpharmacy").show();
                $("#notsentpharmacy").hide();
            } else {
                $("#sentpharmacy").hide();
                $("#notsentpharmacy").show();
            }
         });
 });
 
 
 function removemedicinetablebody(){
    $('#medicinetable tbody tr .input-group').empty();
}

$(".js-select2").select2({
			closeOnSelect : true,
			placeholder : "Enter here...",
			allowHtml: true,
			allowClear: true,
			tags: true
});

$(function () {
        $("#tolab").click(function () {
            if ($(this).is(":checked")) {
                $("#sentlab").show();
                $("#notsentlab").hide();
            } else {
                $("#sentlab").hide();
                $("#notsentlab").show();
            }
         });
});	

 $(function() {
            $( "#datepicker-13" ).datepicker();
			 $( "#datepicker-14" ).datepicker();
 });

$(document).ready(function(){
    $('.deleteRowButton').click(DeleteRow);
	function DeleteRow(){ $(this).parents('tr').first().remove(); };
	
	var i=0;
	$('#add-medicine').click(function(){
		i++;
		$('#dynamic_field').append('<tr id="row'+i+'"><td></td><td class="col-4"><div class="input-group"><input type="text" class="form-control" placeholder="Select Medicine..."  list="list-of-medications" id="medication-list"> <datalist id="list-of-medications"><option>Dolo, 650 mg</option><option>Dolo, 500 mg</option><option>Pan, 40 mg</option><option>Azithro, 500 mg</option></datalist><span class="input-group-text"><i class="fa fa-angle-down" aria-hidden="true"></i></span></div></td><td class="col-1"><div class="input-group"><input type="text" class="form-control" placeholder="Select ..." ><span class="input-group-text"><i class="fa fa-angle-down" aria-hidden="true"></i></span></div></td><td class="col-1"><div class="input-group"><input type="text" class="form-control" placeholder="Select ..." ><span class="input-group-text"><i class="fa fa-angle-down" aria-hidden="true"></i></span></div></td><td class="col-1"><div class="input-group"><input type="text" class="form-control" placeholder="Select ..." ><span class="input-group-text"><i class="fa fa-angle-down" aria-hidden="true"></i></span></div></td><td class="col-1"><div class="input-group"><input type="text" class="form-control" placeholder="Enter ..." ><span class="input-group-text"></span></div></td><td class="col-4"><div class="input-group"><input type="text" class="form-control" placeholder="Enter ..." ></div></td><td ><h5 class="text-danger btn_remove" name="remove" id="'+i+'"><i class="fa fa-times-circle" aria-hidden="true"></i></h5></td></tr>');
	});
	$('#add-investigations').click(function(){
		i++;
		$('#dynamic_investigations').append('<tr id="row'+i+'"><td></td><td class="col-4"><div class="input-group"><input type="text" class="form-control" placeholder="Select Medicine..."  list="list-of-investigations" id="investigations-list"><datalist id="list-of-investigations"><option>D-Dimer</option><option>hemoglobin</option><option>Sugar test</option></datalist><span class="input-group-text"><i class="fa fa-angle-down" aria-hidden="true"></i></span></div></td><td class="col-7"><div class="input-group"><input type="text" class="form-control" placeholder="Enter the remarks ..." ></div></td><td ><h5 class="text-danger btn_remove" name="remove" id="'+i+'"><i class="fa fa-times-circle" aria-hidden="true"></i></h5></td></tr>');
	});
	$(document).on('click', '.btn_remove', function(){
		var button_id = $(this).attr("id"); 
		$('#row'+button_id+'').remove();
	});
	
});