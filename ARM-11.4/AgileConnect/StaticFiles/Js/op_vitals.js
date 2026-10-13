var pageParams = {};
$(document).ready(function () {
    if (typeof window.parent.pageParams.vitalsParams != "undefined") {
        pageParams = Object.assign({}, window.parent.pageParams.vitalsParams);
    }
    else
        pageParams = Object.assign({}, window.parent.pageParams);
});

function generateSaveDataOptions(saveDataOptions) {
    if (saveDataOptions.formId == "op_patientdashboard_vitals_save") {
        saveDataOptions.beforeSave = function () {
            $("#recordid").val(pageParams.inititalassessmentid);
            $(".ax-dc.ax-nongrid[data-ax-dcno='1']").attr("data-ax-dcrowid",pageParams.inititalassessmentid);
            $("#uhid").val(pageParams.uhid);
            $("#op_no_vital").val(pageParams.opno);
            $("#app_no_vital").val(pageParams.appno);
            $("#doctorid").val(pageParams.doctorid);

            var currentdate = new Date();
            var currDatetime = currentdate.getDate() + "/"
                + (currentdate.getMonth() + 1) + "/"
                + currentdate.getFullYear() + " "
                + currentdate.timeNow();
            $("#vital_date").val(currDatetime);
            $("#entrydate").val(currentdate.getDate() + "/"
                + (currentdate.getMonth() + 1) + "/"
                + currentdate.getFullYear());

        };
        saveDataOptions.validateSave = function () {
            var systolic = document.getElementById('systolic');
            var sys = systolic.value;
            sys = Number(sys);
            if (sys >= 100 && sys <= 240) {
                $('input#systolic').removeClass('is-invalid')
                $('input#systolic').addClass('is-valid')
            } else if (systolic.value != "") {
                $('input#systolic').addClass('is-invalid')
                ToastMaker('The systolic between 100-240');
                return false;
            }


            var diastolic = document.getElementById('diastolic');
            var dia = diastolic.value;
            dia = Number(dia);
            if (dia >= 60 && dia <= 100) {
                $('input#diastolic').removeClass('is-invalid')
                $('input#diastolic').addClass('is-valid')

            } else if (diastolic.value != "") {
                $('input#diastolic').addClass('is-invalid')
                ToastMaker('The diastolic between 60-100');
                return false;
            }

            var hrbeats = document.getElementById('hrbeats');
            var hrb = hrbeats.value;
            hrb = Number(hrb);
            if (hrb >= 40 && hrb <= 180) {
                $('input#hrbeats').removeClass('is-invalid')
                $('input#hrbeats').addClass('is-valid')

            } else if (hrbeats.value != "") {
                $('input#hrbeats').addClass('is-invalid')
                ToastMaker('The HR range between 40-180');
                return false;
            }

            var temp = document.getElementById('temp');
            var hrb = temp.value;
            hrb = Number(hrb);
            if (hrb >= 35 && hrb <= 40) {
                $('input#temp').removeClass('is-invalid')
                $('input#temp').addClass('is-valid')

            } else if (hrbeats.value != "") {
                $('input#temp').addClass('is-invalid')
                ToastMaker('The Temperature between 35-40');
                return false;
            }
            
            var height = document.getElementById('height');
            var hei = height.value;
            hei = Number(hei);
            if (hei >= 40 && hei <= 240) {
                $('input#height').removeClass('is-invalid')
                $('input#height').addClass('is-valid')

            } else if (height.value != "") {
                $('input#height').addClass('is-invalid')
                ToastMaker('The height between 40-240');
                return false;
            }

            var weight = document.getElementById('weight');
            var wei = weight.value;
            wei = Number(wei);
            if (wei >= 1 && wei <= 250) {
                $('input#weight').removeClass('is-invalid')
                $('input#weight').addClass('is-valid')

            } else if (weight.value != "") {
                $('input#weight').addClass('is-invalid')
                ToastMaker('The Weight between 1-250');
                return false;
            }

            return true;
        }

        saveDataOptions.afterSave = function (data) {
            var recId = data.result[0].message[0].recordid;
            window.parent.pageParams.inititalassessmentid = recId;
            messageAlert({
                title: "Confirmation",
                message: "Vitals is saved.",
                onclose: function () {
                    setTimeout(function () { window.parent.$("#vitals-capture button.model-close").click(); }, 500);
                    window.parent.$("#btnVitalsChange").click();
                }
            })
        };
    }
    return saveDataOptions;
}

$(document).ready(function () {
    $("[name='height'],[name='weight']").keyup(function (e) {
        e.preventDefault();
        var weight = $("[name='weight']").val();
        var height = $("[name='height']").val();
        if (weight > 0 && height > 0) {
            var finalBmi = (weight / ((height * height) /
                10000)).toFixed(2);
            $("#dopeBMI").val(finalBmi);
            if (finalBmi < 18.4) {
                $("#meaning").val("Underweight");
                return false;
            }
            if (finalBmi > 18.5 && finalBmi < 24.9) {
                $("#meaning").val("Healthy");
                return false;
            }
            if (finalBmi > 25.0 && finalBmi < 29.9) {
                $("#meaning").val("Overweight (Pre-obese)");
                return false;
            }
            if (finalBmi > 30.0 && finalBmi < 34.9) {
                $("#meaning").val("Obese (Class I)");
                return false;
            }
            if (finalBmi > 35.0 && finalBmi < 39.9) {
                $("#meaning").val("Obese (Class II)");
                return false;
            }
            if (finalBmi > 40.0) {
                $("#meaning").val("Obese (Class III)");
                return false;
            }
        } else {
            $("#meaning").val("You are obese.");
            return false;
        }
    });



    var invalidChars = ["-", "e", "+", "E"];
    $("input[type=number]").keypress(function (e) {
        if (invalidChars.includes(e.key)) {
            e.preventDefault();
        }
    })

    $("#txt1hip,#txt1waist").keyup(function (e) {
        e.preventDefault();
        var txtWaist = $('#txt1waist').val();
        var txtHip = $('#txt1hip').val();

        if (txtWaist != '' && txtHip != '') {
            var result = Math.round((txtWaist / txtHip), 2);
            $('#txt1whratio').val(result);
            if (result <= 0.85) {
                $('#txt1whratioremarks').val('Excellent');
            }
            else if (result <= 0.89) {
                $('#txt1whratioremarks').val('Good');
            }
            else if (result <= 0.95) {
                $('#txt1whratioremarks').val('Average');
            }
            else if (result > 0.95) {
                $('#txt1whratioremarks').val('All Risk');
            }
        }
    });

});

Date.prototype.timeNow = function () {
    return ((this.getHours() < 10) ? "0" : "") + ((this.getHours() > 12) ? (this.getHours() - 12) : this.getHours()) + ":" + ((this.getMinutes() < 10) ? "0" : "") + this.getMinutes() + ":" + ((this.getSeconds() < 10) ? "0" : "") + this.getSeconds() + ((this.getHours() > 12) ? (' PM') : ' AM');
};