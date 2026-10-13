exprObj['uhiddocid'] = 'uhid + doctorid';
exprObj['fam_med'] = 'familymember + medicalproblem';

var arrFlds = [];

var fldObj = {};

fldObj["1001"] = { name: "uhid", value: "12345", dcno: "1", dcrowid: 0, dcrowno: 1, gridFld: false };

fldObj["1003"] = { name: "familymember", value: "Father", dcno: "2", dcrowid: 0, dcrowno: 1, gridFld: true };

fldObj["1004"] = { name: "medicalproblem", value: "DA", dcno: "2", dcrowid: 0, dcrowno: 1, gridFld: true };


fldObj["1005"] = { name: "familymember", value: "Mother", dcno: "2", dcrowid: 0, dcrowno: 2, gridFld: true };

fldObj["1006"] = { name: "medicalproblem", value: "HT", dcno: "2", dcrowid: 0, dcrowno: 2, gridFld: true };


fldDcMapping = {};

fldDcMapping["uhid"] = '1';
fldDcMapping["doctorid"] = '1';
fldDcMapping["familymember"] = '2';
fldDcMapping["medicalproblem"] = '2';

dcGridDcMapping = {};
dcGridDcMapping["1"] = false;
dcGridDcMapping["2"] = true;

dcRowsMapping = {};
dcRowsMapping["1"] = ['dc1001'];
dcRowsMapping["2"] = ['dc2001', 'dc2002', 'dc2003'];

dcDataObj["dc1001"] = [
    { name: "uhid", value: "12345", dcrowid: 0, dcrowno: 1},
    { name: "doctorid", value: "100033445566", dcrowid: 0, dcrowno: 1 },
    { name: "uhiddocid", expr:"uhid + doctorid"}
];

dcDataObj["dc2001"] = [
    { name: "familymember", value: "Father", dcrowid: 0, dcrowno: 1},
    { name: "medicalproblem", value: "DA", dcrowid: 0, dcrowno: 1 },
    { name: "fam_med", expr: "familymember + medicalproblem" },
    { name: "doc_med", expr: "doctorid + medicalproblem" }
];

dcDataObj["dc2002"] = [
    { name: "familymember", value: "Mother", dcrowid: 0, dcrowno: 2},
    { name: "medicalproblem", value: "HT", dcrowid: 0, dcrowno: 2 },
    { name: "fam_med", expr: "familymember + medicalproblem" },
    { name: "doc_med", expr: "doctorid + medicalproblem" }
];

uhid001F1

Expression1 = uhid + doctorid
uhiddocid.dcDataObj = "dc1001";

Expression2 = familymember + medicalproblem
Expression3 = doctorid + medicalproblem
Expression4 = total(familymember)