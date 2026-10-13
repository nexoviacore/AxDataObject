var stepIds = [1, 2, 3, 4,5 ]
  $(document).ready(function(){
  var current = 1,current_step,next_step,steps;
  steps = $("fieldset").length;
  //$(".next").click(function(){
  	$(document).on("click",".next",function(){
    current_step = $(this).closest("fieldset").data("id");
    next_step = stepIds[stepIds.indexOf(current_step)+1];
    $('fieldset[data-id="' + current_step + '"]').hide();
    $('fieldset[data-id="' + next_step + '"]').show();
  });
  	$(document).on("click",".previous",function(){
    current_step = $(this).closest("fieldset").data("id");
    next_step = stepIds[stepIds.indexOf(current_step)-1];
    $('fieldset[data-id="' + current_step + '"]').hide();
    $('fieldset[data-id="' + next_step + '"]').show();
  });
  $(document).on("click",".step-3",function(){
    current_step = $(this).closest("fieldset").data("id");
    next_step = stepIds[stepIds.indexOf(3)];
    if (next_step) {
      $('fieldset[data-id="' + current_step + '"]').hide();
      $('fieldset[data-id="' + next_step + '"]').show();
    }
  });
  $(document).on("click",".step-4",function(){
    current_step = $(this).closest("fieldset").data("id");
    next_step = stepIds[stepIds.indexOf(4)];
    if (next_step) {
      $('fieldset[data-id="' + current_step + '"]').hide();
      $('fieldset[data-id="' + next_step + '"]').show();
    }
  });
});