unit uASBTStructObj;
{copied from 11.3/AsbSoap}
interface

uses  Classes , SysUtils , XMLDoc, XMLIntf , Variants , uDBManager , uConnect , uAxProvider ,
      uXds, uParse , uDbCall , uDoCoreAction,  DateUtils , uASBCommonObj, uStructDef,
      uASBDataObj , uWorkFlowRuntime , uSearchVal , uGeneralFunctions, uStoreData,uProfitEval , uDataExport ,
      uDataImport , System.StrUtils, DB,uprintdocs , uValidate , uPropsXML,idGlobal;

type
  TFGCall=(fgFromFormLoad, fgFromButtonClick, fgFromDependents, fgFromLoadDC);

  TASBTStructObj = class
  PRIVATE
    servicename,tfile,spath,close_err,tcstr,VisibleDCs :String;
    XMLDoc , StructXml : IXMLDocument;
    xnode : ixmlnode;
    openConnect,trace : boolean;
    dbm : TDBManager;
    AxProvider : TAxProvider;
    connection : pConnection;
    ASBCommonObj : TASBCommonObj ;
    ASBDataObj : TASBDataObj;
    act : TDoCoreAction;
    dbCall,WithDbCall : TDbCall;
    fld : pFld;
    dbTransid,oldpval  : String;
    Sdef : TstructDef;
    FastDataFlag : Boolean;
    appsessionkey : AnsiString;
    GenerateSessionAppKey , ValidateSessionAppKey,SetDefaultValues,GetServerDT : Boolean;

    function LoadXMLDataFromWS(xml: WideString): IXMLDocument;
    procedure ConnectToProject(db: String);
    function CloseProject : String;
    function GetQoutedVisibleDCNames(dcname: String): String;
    procedure FindAndExeFormLoadAction ;
    procedure FindAndExeDataLoadAction;
    function ValidatePickListValue(fldName: String; fno, actrow: integer;
      flist: TStringList): String;
    procedure DeletePopFldsFromDepFieldList(flist: TStringlist);
    procedure FindandClearFG(frmName: String);
    function FindandExecuteFG(frmName, callfrom: String): boolean;
    procedure FillPopUpForDisplayField(fldName : String ; fno,rowno : integer);
    function FillPopUpForShowBtnField(fldName : String ; fno,rowno : integer) : boolean;
    procedure ProcessPopField(fn: integer);
    procedure WriteResultForGrid(q: TXDS; rnode, fgnode: ixmlnode;fs,colsize:String);
    procedure MakeResponse(v: String; mn, enode: ixmlnode);
    procedure WriteResult(q: TXDS; rnode: ixmlnode);
    function InsertCondition(sqltext, val: String): String;
    function AssignParams(Q: TXDS): boolean;
    procedure WriteResultForGetFieldValue(q: TXDS;rnode : ixmlnode;dynamicfilter : boolean;map : AnsiString);
    function GetActiveRowDepentendList(fname: String): String;
    procedure DeleteInvalidPopGridRows;
    function WriteResultForGetSearchResult(fld: pFld; q: TXDS; scol: integer) : String;
    function MakeSearchResult(fld : pFld ; sqlfld , v,SearchCond: String; q: tXDS): String;
    function LoadTrans(Transid: String; Recordid: Extended): String;
    function EndTrans: String;
    function SaveTrans: String;
    function SaveDataInWFAction: String;
    function SubmitAndValidateInWFAction(x: String): String;
    procedure WriteResultWithCap(q: TXDS;rnode : ixmlnode);
    procedure CopyQResult(q: TXDS;popgrid:pPopGrid);
    procedure DeleteExistingRows(FldName, OldVal, NewVal: String; popindex,
      rowno: integer; popgrid: pPopgrid);
    procedure InsertParentDetails(RowNo: integer);
    procedure PopupAutoFill(FldName, OldVal, NewVal: String; popindex,
      rowno: integer);
    procedure GetParentValue(fldName: String; fno, rno: integer);
    function CheckAndFillPopUpForParentField(fldName: String; fno,
      rowno: integer): boolean;
    function ChangeSQL(SqlText,SearchValue:String;SearchFldList:TStringList;SearchIdx:Integer; SearchCond,SearchValue2 : string):String;
    procedure GetPickListResult(fld : pFld; sqlfld , val : String; SearchCond:string=''; SearchVal2 : string = '');
    function MakeProperValue(Str: String): String;
    function CreateErrNode(errmsg: String; xmlresult: boolean): String;
    function FindAndReplace(S, FindWhat, ReplaceWith: String): String;
    function GetPopGridDcs(flist: TStringList): String;
    function FindParentField(fldName: String; fno: integer): String;
    procedure FormLoadJSON;
    procedure LoadDCJSON(FrameNo: Integer;nofill : boolean);
    function IsFGBound(fg: pfg): boolean;
    function ExecFillGrid(FrameNo: Integer; callfrom: TFgCall;fgName : String): pFg;
    procedure LoadDataJSON;
    procedure GetDependents(FieldName:String; RowNo:Integer);
    function NonGridToGrid(fd, dfd: pFld) : string;
    function RefreshPopup(PopIndex: integer; fd: pFld; RowNo: integer) : string ;
    function PopAtField(dfd: pFld): integer;
    procedure RefreshRow(fd: pFld; RowNo: Integer);
    function DoPopup(FrameNo, RowNo: Integer):String;
    function DepFillGrid(FrameNo: Integer): String;
    procedure AddGridRow(FrameNo, RowNo: Integer);
    function IndirectDepFillGrid(FrameNo: Integer): String;
    function MakeSearchResultForGetDep(fld: pFld; sqlfld, v, idupdate: String;
      q: TXDS): String;
    function MakeResultForGetDep(fld: pFld; sqlfld, v, idupdate: String;
      q: TXDS): String;
    procedure PopGridJSON(pframeno ,prowcount: integer);
    procedure LoadSubGridJSON(FrameNo: Integer);
    procedure CreateRecIdDataNode;
    function CreateHistoryData(tstrlst: TstringList;xd :IXMLDocument ): IXMLDocument;
    procedure CreateNewFormatHistoryData(history_slist : TStringList; transid,recordid : string);
    function GetRecordIDtoLoad(n: ixmlnode): extended;
    procedure GridDependentOtherGridFieldsToJSON(
      GridDependentOtherGridFields: String);
    procedure GridDependentOtherGridToJSON(FrameNo: integer;
      GridDependentOtherGridFields: String);
    function RefreshDependentFGs : String;
    procedure CopyTransAndSave(SrcTransid: String; SrcRecid: Extended;
      FldDets: String);
    function SendMail(XmlString:IXMlNode):String;
    function ChangeFormatForSplitresult(str,Param: string): String;
    function GetFillgriddefDetails(transid, fillgridname:string): string;
    function AddToAppVars: String;
    procedure GetAutoCompleteFillData(Fld:pFld; q:TXDS; var DepTrnFldList: TStringList; var DepQryFldList :TStringList);
    function CreateResultWithAppSessionKey(xmldoc: ixmldocument;
      jsonresult: widestring): WideString;
    function MakeSearchResultNew(sqlfld, v, SearchCond: String;
      q: TXDS; Validate : TValidate): String;
    procedure GetPickListResultNew(sqlfld, val, SearchCond: string; q : TXDS; Validate : TValidate);
    function WriteResultForGetSearchResultNew(sqlfld : string ; q: TXDS;scol: integer;SourceKey : boolean): String;
    function GetPickListFields(sqlfld,sqltext: string ; pickfields : TStringList): TStringList;
    procedure GetAutoCompleteFillDataNew(sqlfld : string; q: TXDS; Dependents: TStringList;
      var DepTrnFldList, DepQryFldList: TStringList);
    function ChangeSQLNew(SqlText, SearchValue: String;
      SearchFldList: TStringList; SearchIdx: Integer; SearchCond,
      SearchValue2: string; Validate : TValidate): String;
    function FillCompanyParams(DynamicSQL: String; Validate: TValidate): string;
    function FillCompositeParams(S: String; Validate : TValidate): String;

    function DoFormLoadNew(xml, sxml , formload_fldlist: string): string;
    procedure NewFormLoadJSON(fldlist : string);
    function LoadDCCombosNew(xml, sxml,formload_fldlist: string): string;
    procedure LoadDCJsonNew(FrameNo: Integer; nofill: boolean);
    function GetNthString(SrcString: String; StrPos: integer): String;
    function GetDepentendFieldValuesNew(xml, sxml,formload_fldlist: string): string;
    procedure GetDependentsNew( RowNo: integer;fd:pFld);
    function LoadDataNew(s, sxml,nonsaveflds: String): string;
    procedure LoadDataJSONNew;
    procedure PrepareNonSaveFlds(nonsaveflds: string);
    function AddGridRowValuesNew(xml, sxml,formload_fldlist: string): string;
    procedure AddGridRowNew(FrameNo, RowNo: Integer);
    function DeleteGridRowValuesNew(xml,sxml,formload_fldlist: string): string;
    function DoFillGridValuesNew(xml, sxml, formload_fldlist: string): string;
    procedure RefreshRowNew(fd: pFld; RowNo: Integer);
    function DepFillGridNew(FrameNo: Integer ; depflds : TStringList): String;
    procedure CreateNonSaveFlds(nonsaveflds: string);
    function NonGridToGridNew(fd, dfd: pFld): string;
    function RefreshGridDependentsNew(xml, sxml,formload_fldlist: string): string;
    function GlobalVarsInSqls : String;
    function MakeMultiSelectList(sqlfld, v, SearchCond: String; q: TXDS;
      Validate: TValidate): String;
    function WriteResultForGetMultiSelectValues(sqlfld: string; q: TXDS;
      scol: integer; SourceKey: boolean): String;
    procedure GetWebAxApps(db: ansistring);

    function AddFillFldToList(xml : IXMLDocument ; pfld : ansistring) : AnsiString;
    procedure RegisterAppValues(Parser:TEVal);
    procedure RegisterGlobalVarsforDBFunc(Parser:TEVal);
    function SDEvaluateExpr(sExpression: String): String;
    procedure WriteSQLResultForGetFieldValue(q: TXDS; rnode: ixmlnode;
      dynamicfilter: boolean;map : AnsiString);
    procedure SDRegVarToParser(pVarname, pVarDType, pVarValue: String);
    procedure ProcessRMQMessages;
  public
    function FastDoFormLoad(xml, sxml: string): string;
    function FastLoadData(xml, sxml: String): string;
    function FastGetDepentendFieldValues(xml, sxml: string): string;
    function FastLoadDCCombos(xml, sxml: string): string;
    function FastDoFillGridValues(xml, sxml: string): string;
    function FastGetSearchResult(xml, sxml: string): string;
    function FastFieldChoices(xml,sxml: string): widestring;

    function DoFormLoad(xml, sxml: string): string;
    function GetDepentendFieldValues(xml, sxml: string): string;
    function LoadData(s, sxml: String): string;
    function LoadDataFromHTML(s, sxml: String): string;
    function SaveData(xml, sxml: string): string;
    function DeleteData(S: String): String;
    function GetFillGridValues(xml, sxml: string): string;
    function DoFillGridValues(xml, sxml: string): string;
    function GetFieldChoices(s,sxml: string): widestring;
    function AddGridRowValues(xml,sxml: string): string;
    function DeleteGridRowValues(xml,sxml: string): string;
    function LoadDCCombos(xml, sxml: string): string;
    function SetAtachments(S , sxml : String): String;
    function ViewAtachments(S, sxml: String): String;
    function RemoveAtachments(S, sxml: String): String;
    function GetHistoryData(S, sxml: string): widestring;
    function WorkFlowAction(xml: string): string;
    function ViewComments(xml: string): string;
    function GetSearchResult(s,sxml: string): string;
    function GetSearchVal(s,sxml: string): WideString;
    function GetRecordId(xml: string): string;
    function DeleteRows(S: String): String;
    function RefreshGridDependents(xml, sxml: string): string;
    function GetSubGridDropDown(xml, sxml: string): string;
    function RefreshDC(xml, sxml: string): string;
    function GetWFButtons(xml: string): string;
    function StartWFAction(xml: string): string;
    constructor Create;
    destructor Destroy; override;
    function ExportData(S: String): WideString;
    function ImportData(S: String): WideString;
    function UnlockTstructsRecord(xml: string): string;
    function AutoGetSearchResult(s, sxml: string): string;
    function AutoGetSearchResultNew(s, sxml: string): string;
    function GetMultiSelectValues(s, sxml: string): string;
    function GetAxMemLoadVars(xml: string): string;
    function DoExcelImportValues(xml, sxml: string): string;
  end ;

implementation

{ TASBTStructObj }

constructor TASBTStructObj.Create;
begin
  tfile := '';
  servicename:='';
  spath := '';
  sdef := nil;
  FastDataFlag := false;
  GenerateSessionAppKey := True;
  ValidateSessionAppKey := True;
  SetDefaultValues := true;
  GetServerDT := true;
  StructXml := nil;
end;

destructor TASBTStructObj.Destroy;
begin
  inherited;
end;

function TASBTStructObj.LoadXMLDataFromWS(xml : WideString) : IXMLDocument;
begin
   result := LoadXMLData(xml);
end;

procedure TASBTStructObj.ConnectToProject(db:String);
  var x : AnsiString;
begin
  ASBDataObj := nil;
  ASBCommonObj := tASBCommonObj.Create;
  ASBCommonObj.tfile := tfile;
  ASBCommonObj.XMLDoc := XMLDoc;
  ASBCommonObj.openConnect := openConnect;
  ASBCommonObj.spath := spath;
  ASBCommonObj.servicename := servicename;
  ASBCommonObj.ValidateSessionAppKey := ValidateSessionAppKey;
  dbm :=  ASBCommonObj.CreateDbManager(db);
  dbm.SetDefaultValues := SetDefaultValues;
  dbm.GetServerDT := GetServerDT;
  dbm.gf.AutoGenData := nil;
  ASBCommonObj.ConnectToDB(db);
  x := vartostr(ASBCommonObj.XMLDoc.documentelement.attributes['webaxpapp']);
  if x <> '' then GetWebAxApps(x);
  if (pos('tstruct',ASBCommonObj.debugcomp) > 0) and (not dbm.gf.DoDebug.Active) then
  begin
    dbm.gf.DoDebug.debugcomp := 'tstruct';
    dbm.gf.dodebug.msg('ASB 8.X.Ver DLL running');
    dbm.gf.dodebug.msg('DB Manager created');
    dbm.gf.dodebug.msg('Current AWS folder : ' + dbm.gf.exepath);
    dbm.gf.dodebug.msg('TStruct DLL Error debug on');
  end;
  AxProvider := ASBCommonObj.AxProvider;
  connection := ASBCommonObj.connection;
  ASBDataObj := TASBDataObj.Create;
  ASBDataObj.dbm := dbm ;
  ASBDataObj.Axprovider := AxProvider;
  ASBDataObj.connection := connection;
  ASBDataObj.ASBCommonObj := ASBCommonObj;
  ASBDataObj.XMLDoc := XMLDoc;
  ASBDataObj.FastDataFlag := FastDataFlag;
  dbm.gf.AutoGenData := TList.Create;
end;

procedure TASBTStructObj.GetWebAxApps(db : ansistring);
   var n : ixmlnode;
       k,i : integer;
       found : boolean;
begin
  n := XMLDoc.DocumentElement.ChildNodes.FindNode(db);
  if assigned(n) then
  begin
    k := 0;
    found := true;
    if n.HasAttribute('rowno') then
    begin
       found := false;
       for i := 0 to XMLDoc.DocumentElement.ChildNodes.Count - 1 do
       begin
         n := XMLDoc.DocumentElement.ChildNodes[i];
         if n.NodeName = db then
         begin
           if not n.HasAttribute('rowno') then
           begin
             found := true;
             dbm.gf.WebAppConXML := LoadXMLData('<root></root>');
             dbm.gf.WebAppConXML.DocumentElement := n.CloneNode(true);
             XMLDoc.DocumentElement.ChildNodes.Delete(i);
             k := i;
             break;
           end;
         end;
       end;
    end
  end else found := false;
  dbm.gf.dodebug.msg('Connecting to project found : ' + booltostr(found));
  if found then
  begin
    if k = 0 then
    begin
      dbm.gf.WebAppConXML := LoadXMLData('<root></root>');
      dbm.gf.WebAppConXML.DocumentElement := XMLDoc.DocumentElement.ChildNodes.FindNode(db).CloneNode(true);
      XMLDoc.DocumentElement.ChildNodes.Delete(db);
      dbm.webAppCon := db;
    end;
  end;
end;

function TASBTStructObj.CloseProject : String;
  var  ServiceStartTtime : TDateTime;
       dbtime : integer;
       timetaken : String;
begin
  result := '';
  if not assigned(ASBCommonObj) then exit;
  ServiceStartTtime := ASBCommonObj.ServiceStartTtime;
  dbTime := dbm.gf.dbtimetaken;
  try
    if assigned(ASBDataObj) then
    begin
      if assigned(ASBDataObj.dbcall) then
      begin
         ASBCommonObj.structname := ASBDataObj.dbcall.transid;
         if (ASBCommonObj.structrecord = '') or (ASBCommonObj.structrecord = '0') then
            ASBCommonObj.structrecord := FloatToStr(ASBDataObj.dbcall.LastSavedId);
      end;
      ASBDataObj.Destroy;
    end;
    if assigned(dbm.gf.AutoGenData) then
    begin
      dbm.gf.ClearAutoGenData(True);
      FreeAndNil(dbm.gf.AutoGenData);
    end;

    ASBCommonObj.servicename := servicename;
    ASBCommonObj.GenerateSessionAppKey := GenerateSessionAppKey;
    ASBCommonObj.CloseProject;
    close_err := ASBCommonObj.close_err;
    appsessionkey := ASBCommonObj.appsessionkey;
    dbTime := dbTime + ASBCommonObj.dbtimetaken;
  except
  end;
  try
    ASBCommonObj.Destroy;
  finally
    timetaken := timetaken + '{"timetaken":[';
    timetaken := timetaken + '{"total":"'+inttostr(millisecondsbetween(now(),ServiceStartTtime))+'"}';
    timetaken := timetaken + ',{"dbtime":"'+inttostr(dbtime)+'"}';
    timetaken := timetaken + ']}';
    result := timetaken ;
  end;
end;

function TASBTStructObj.DoFormLoad(xml, sxml:string):string;
var x,formload_fldlist : String;
    loadstruct : boolean;
    recid:extended;
    n : ixmlnode;
    stime:TDateTime;
begin
  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := DoFormLoadNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;
  servicename:='Form Load';
  stime:=now;
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DoFormLoad');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing DoFormLoad webservice');
    dbm.gf.dodebug.msg('-------------------------------');
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      dbm.gf.dodebug.msg('Received XML ' + xml);
      dbm.gf.dodebug.msg('Received SXML ' + sxml);
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,sxml,StructXml);
      dbCall := ASBDataObj.DbCall;
      if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
      ASBDataObj.SubmitClientValuesToSD;
      ASBDataObj.CreateActionObj(x,'tstructs');
      loadstruct := true;
      if xmldoc.documentelement.HasAttribute('act') then
      begin
         if vartostr(xmldoc.documentelement.attributes['act']) = 'open' then
            loadstruct := false;
      end;
      if xmldoc.documentelement.ChildNodes.Count = 0 then loadstruct := false;
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      stime:=now;
      if loadstruct = false then
      begin
        dbCall.Parser.RegisterVar('ActiveRow', 'n', '1');
        FindAndExeFormLoadAction ;
        FormLoadJSON;
        Result := ASBDataObj.GetJSON;
      end else
      begin
        if (tcstr <> 'n') then
        begin
          n := xmldoc.DocumentElement;
          recid := GetRecordIDtoLoad(n);
          if recid>0 then
          begin
             dbm.gf.dodebug.msg('Record ID : ' + floattostr(recid));
             dbcall.LoadData(recid);
             ASBDataObj.act.RecId := floattostr(recid);
             FindAndExeFormLoadAction;
             FindAndExeDataLoadAction;
          end;
          LoadDataJSON;
          Result := ASBDataObj.GetJSON;
        end;
      end;
      dbm.gf.dodebug.msg('Result : ' + Result);
      dbm.gf.dodebug.msg('Time taken by FormLoad - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('DoFormLoad webservice completed');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.GetDepentendFieldValues(xml,sxml:string):string;
var x,fldname,plistval,s,formload_fldlist: String;
    i,fno,actrow,pidx,pactrow : integer;
    pickfield : boolean;
begin

  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := GetDepentendFieldValuesNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;

  servicename:='Get Dependents';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetDepentendFieldValues') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to webservice GetDepentendFieldValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetDepentendFieldValues webservice');
    dbm.gf.dodebug.msg('--------------------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession ;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      fldname := xmldoc.DocumentElement.Attributes['field'];
      dbm.gf.dodebug.msg('Creating DBCall');
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbm.gf.dodebug.msg('DBCall created');
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      fno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
         fno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['frameno']));
      actrow := 0 ;
      if vartype(xmldoc.DocumentElement.Attributes['activerow']) <> varnull then
         actrow := strtoint(vartostr(xmldoc.DocumentElement.Attributes['activerow']));
      if actrow = 0 then actrow := 1;
      plistval := 'yes';
      pickfield := false;
      if vartype(xmldoc.DocumentElement.Attributes['plistval']) <> varnull then
      begin
         if vartostr(xmldoc.DocumentElement.Attributes['plistval']) <> '' then
             plistval := vartostr(xmldoc.DocumentElement.Attributes['plistval']);
      end;
      if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
      begin
         if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
            pactrow := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
         dbCall.Parser.RegisterVar('activeprow', 'n', inttostr(pactrow));
      end;
      if result = '' then
      begin
        GetDependents(fldname, actrow);
        result:=ASBDataObj.GetJSON;
      end;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing GetDepentendFieldValues webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.LoadData(s,sxml: String): string;
var x , formload_fldlist,loadflag : String;
    f:extended;
    datavalidate : boolean;
    i : integer;
begin
  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      loadflag := '';
      i := pos('~',formload_fldlist);
      loadflag := copy(formload_fldlist,1,i-1);
      if loadflag = '' then raise Exception.create('Quick Load Data Structure XML not valid...');
      formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['nonsaveflds']);
      result := LoadDataNew(s,sxml,formload_fldlist) ;
      exit;
    end;
  end;
  result := 'done';
  servicename:='Load Data';
try
  xmldoc := LoadXMLDataFromWS(s);
  if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
    raise Exception.create('Sessionid not specified in LoadData');
  if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
    raise Exception.create('axpapp tag not specified in to loaddata webservice');
  if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
    raise Exception.create('transid tag not specified in parameter');
  if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
    raise exception.create('recordid attribute not specified');
  if xmldoc.DocumentElement.HasAttribute('trace') then
     tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
  if xmldoc.DocumentElement.HasAttribute('scriptpath') then
     spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
  x := xmldoc.documentelement.attributes['axpapp'];
  openConnect := False;
  ConnectToProject(x);
  dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
  dbm.gf.dodebug.msg('Executing LoadData webservice');
  dbm.gf.dodebug.msg('-----------------------------');
  dbm.gf.dodebug.msg('Received XML : ' + s);
  x := ASBCommonObj.ValidateSession;
  ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
  if copy(x,1,7) = '<error>' then
     result := CreateErrNode(x,false)
  else
  begin
    datavalidate := true;
    if vartype(xmldoc.DocumentElement.Attributes['dataval']) <> varnull then
      if vartostr(xmldoc.DocumentElement.Attributes['dataval']) <> '' then
       datavalidate := strtobool(vartostr(xmldoc.DocumentElement.Attributes['dataval']));
    if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
    begin
       VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
       ASBDataObj.VisibleDCs := VisibleDCs;
    end;
    x:= xmldoc.DocumentElement.Attributes['transid'];
    ASBDataObj.CreateAndSetDbCall(x,sxml);
    ASBDataObj.DbCall.dataval := datavalidate;
    dbCall := ASBDataObj.DbCall;
    if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
    ASBDataObj.CreateActionObj(x,'tstructs');
    ASBDataObj.SubmitClientValuesToSD;
    ASBCommonObj.structrecord := vartostr(xmldoc.DocumentElement.attributes['recordid']);
    x := xmldoc.DocumentElement.attributes['recordid'];
    dbm.gf.doDebug.Msg('Record id : ' + x);
    f := dbm.gf.strtofloatz(x);
    if f>0 then
    begin
      if Assigned(dbcall.StoreData) then
      begin
        //if the call is from peg editable form , then do not init peg / amendment flow
        if vartype(xmldoc.DocumentElement.Attributes['ispegedit']) <> varnull then
        begin
           if lowercase(vartostr(xmldoc.DocumentElement.Attributes['ispegedit'])) = 'true' then
           begin
            dbcall.StoreData.IsPegEdit := true;
           end;
        end;
        dbcall.StoreData.SDEvaluateExpr := SDEvaluateExpr;
        dbcall.StoreData.SDRegVarToParser := SDRegVarToParser;
        dbcall.StoreData.Object_ASBDataObj := ASBDataObj;
        dbcall.StoreData.ParserObject := dbcall.Parser;
      end;
      dbcall.LoadData(f);
      if (tcstr <> 'n') then
      begin
        FindAndExeDataLoadAction;
        LoadDataJSON;
        Result := ASBDataObj.GetJSON;
        dbm.gf.dodebug.msg('Result : ' + Result);
      end;
    end;
  end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
      begin
        dbm.gf.execActName := 'LoadData';
        if assigned(dbcall) then dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        dbm.gf.execActName := '';
      end;
    end;
  end;
  dbm.gf.dodebug.msg('LoadData webservice completed');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x := closeproject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.LoadDataFromHTML(s,sxml: String): string;
var x,mustloadfld : string;
    f:extended;
    datavalidate : boolean;
begin
  result := 'done';
  servicename:='Load Data From HTML';
try
  xmldoc := LoadXMLDataFromWS(s);
  if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
    raise Exception.create('Sessionid not specified in LoadDataFromHTML');
  if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
    raise Exception.create('axpapp tag not specified in to LoadDataFromHTML webservice');
  if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
    raise Exception.create('transid tag not specified in parameter');
  if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
    raise exception.create('recordid attribute not specified');
  if xmldoc.DocumentElement.HasAttribute('trace') then
     tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
  if xmldoc.DocumentElement.HasAttribute('scriptpath') then
     spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
  x := xmldoc.documentelement.attributes['axpapp'];
  openConnect := False;
  ConnectToProject(x);
  dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
  dbm.gf.dodebug.msg('Executing LoadDataFromHTML webservice');
  dbm.gf.dodebug.msg('-----------------------------');
  dbm.gf.dodebug.msg('Received XML : ' + s);
  x := ASBCommonObj.ValidateSession;
  if copy(x,1,7) = '<error>' then
     result := CreateErrNode(x,false)
  else
  begin
    datavalidate := true;
    if vartype(xmldoc.DocumentElement.Attributes['dataval']) <> varnull then
      if vartostr(xmldoc.DocumentElement.Attributes['dataval']) <> '' then
       datavalidate := strtobool(vartostr(xmldoc.DocumentElement.Attributes['dataval']));
    if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
    begin
       VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
       ASBDataObj.VisibleDCs := VisibleDCs;
    end;
    x:= xmldoc.DocumentElement.Attributes['transid'];
    ASBDataObj.CreateAndSetDbCall(x,sxml);
    ASBDataObj.DbCall.dataval := datavalidate;
    dbCall := ASBDataObj.DbCall;
    if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
    ASBDataObj.CreateActionObj(x,'tstructs');
    ASBDataObj.SubmitClientValuesToSD;
    ASBCommonObj.structrecord := vartostr(xmldoc.DocumentElement.attributes['recordid']);
    x := xmldoc.DocumentElement.attributes['recordid'];
    dbm.gf.doDebug.Msg('Record id : ' + x);
    f := dbm.gf.strtofloatz(x);
    if f>0 then
    begin
      if (tcstr <> 'n') then
      begin
        FindAndExeDataLoadAction;
        mustloadfld := ASBDataObj.GetMustLoadFields;
        ASBDataObj.GetMustLoadFieldValues(mustloadfld,x);
        ASBDataObj.EndDataJSON;
        Result := ASBDataObj.GetJSON;
        dbm.gf.dodebug.msg('Result : ' + Result);
      end;
    end;
  end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
      begin
        dbm.gf.execActName := 'LoadData';
        if assigned(dbcall) then dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        dbm.gf.execActName := '';
      end;
    end;
  end;
  dbm.gf.dodebug.msg('LoadDataFromXML webservice completed');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

//SDEvaluateExpr
Function TASBTStructObj.SDEvaluateExpr(sExpression : String):String;
begin
  result := '';
  dbm.gf.dodebug.msg('SDEvaluateExpr starts...');
  if Assigned(dbcall) and Assigned(dbcall.Parser) then
  begin
     dbm.gf.dodebug.msg('Evaluating Expression :'+sExpression);
     dbCall.Parser.Evaluate(sExpression);
     result := dbCall.Parser.Value;
     dbm.gf.dodebug.msg('Result of Expression :'+result);
  end
  else
    dbm.gf.dodebug.msg('dbcall/dbcall.Parser is not assigned.');
  dbm.gf.dodebug.msg('SDEvaluateExpr ends.');
end;

//SDRegVarToParser
Procedure TASBTStructObj.SDRegVarToParser(pVarname,pVarDType,pVarValue : String);
begin
  dbm.gf.dodebug.msg('SDRegVarToParser starts...');
  if Assigned(dbcall) and Assigned(dbcall.Parser) then
  begin
     dbm.gf.dodebug.msg('Variablename :'+pVarname);
     dbm.gf.dodebug.msg('Variablevalue :'+pVarValue);
     dbCall.Parser.RegisterVar(pVarname, pVarDType[1], pVarValue);
  end
  else
    dbm.gf.dodebug.msg('dbcall/dbcall.Parser is not assigned.');
  dbm.gf.dodebug.msg('SDRegVarToParser ends.');
end;

//ProcessRMQMessages | process RMQ messages After commit
Procedure TASBTStructObj.ProcessRMQMessages;
begin
  dbm.gf.dodebug.msg('ASBTStructObj/ProcessRMQMessages starts...');
  try
    //Process PEG
    if Assigned(dbcall.StoreData) and Assigned(dbcall.StoreData.AxPEG) then
    begin
      dbm.gf.dodebug.msg('Processing PEG RMQ messages...');
      dbcall.StoreData.AxPEG.PushMessagesToRMQ;
    end;
    //Process Form Notificaitons
    if Assigned(dbcall.FormNotify) then
    begin
      dbm.gf.dodebug.msg('Processing Form notification RMQ messages...');

      //PrepareVars
      if Assigned(ASBCommonObj) then
      begin
        dbcall.FormNotify.globalvars :=  ASBCommonObj.globalvars;
        dbcall.FormNotify.uservars :=  ASBCommonObj.uservars;
        dbcall.FormNotify.axprops :=  ASBCommonObj.axprops;
        dbcall.FormNotify.axapps :=  ASBCommonObj.axapps;
      end;
      dbcall.FormNotify.PushMessagesToRMQ;
    end;
  Except on E:Exception do
    dbm.gf.dodebug.msg('Error in ASBTStructObj/ProcessRMQMessages : '+E.Message);
  end;
  dbm.gf.dodebug.msg('ASBTStructObj/ProcessRMQMessages ends.');
end;

function TASBTStructObj.SaveData(xml,sxml :string):string;
var x , transcap , f , v, tname, iname,s , uname,pwd : String;
    n,delrowsnode : ixmlnode;
    i ,client_timeout : integer;
    recid : Extended;
    delrowsxml : ixmldocument;
    SendRecIdsToClient : boolean;
    splitresult:Boolean;
    prnt : TPrintDocs;
    starttime:TDateTime;
    time_used : Extended;
begin
  starttime:=now();
  result := '';
  uname := '';pwd := '';
  prnt := nil;
  splitresult := True;
  servicename:='Saving data';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice savedata');
    xnode := xmldoc.DocumentElement.ChildNodes.FindNode('data');
    if xnode = nil then
      raise Exception.create('data tag not specified in call to webservice savedata');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    if xmldoc.DocumentElement.HasAttribute('cap') then
      transcap :=  vartostr(xmldoc.documentelement.attributes['cap'])
    else
      transcap := '';
    if xmldoc.DocumentElement.HasAttribute('splitresultforclient') then
    begin
      if vartype(xmldoc.DocumentElement.Attributes['splitresultforclient']) <> varnull then
         splitresult := xmldoc.DocumentElement.Attributes['splitresultforclient'];//new
    end;
    if vartype(xmldoc.DocumentElement.Attributes['password']) <> varnull then
    begin
      if vartype(xmldoc.DocumentElement.Attributes['username']) <> varnull then
      begin
       uname := vartostr(xmldoc.DocumentElement.Attributes['username']);
      end;
      pwd := vartostr(xmldoc.DocumentElement.Attributes['password']);
      if (uname <> '') and (pwd <> '') then ValidateSessionAppKey := False;
    end;
    client_timeout := 0;
    if vartype(xmldoc.DocumentElement.Attributes['timeout']) <> varnull then
    begin
       if isnumeric(vartostr(xmldoc.DocumentElement.Attributes['timeout'])) then
       client_timeout := strtoint(vartostr(xmldoc.DocumentElement.Attributes['timeout']));
    end;
    openConnect := True;
    ConnectToProject(x);
    dbm.gf.sessionid := '';
    SendRecIdsToClient := false;
    if vartype(xmldoc.DocumentElement.Attributes['axp_recid']) <> varnull then
    begin
       if vartostr(xmldoc.DocumentElement.Attributes['axp_recid']) = 'T' then
          SendRecIdsToClient := true;
    end;
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) <> varnull then
       dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Savedata webservice');
    dbm.gf.dodebug.msg('-----------------------------');
    dbm.gf.dodebug.msg('Received XML : ' + xml);
    ASBCommonObj.licCheck := True;
    x := ASBCommonObj.ValidateSession;
//    dbm.gf.DoDebug.writelog := true;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartype(xmldoc.DocumentElement.Attributes['imagefromdb']) <> varnull then
         if vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']) <> '' then
          ASBDataObj.imagefromdb := strtobool(vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']));
      x:= xnode.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      ASBDataObj.DbCall.CreateMapObjects;
      dbCall := ASBDataObj.DbCall;
      dbCall.imagesavetodb := ASBDataObj.imagefromdb;
      dbm.gf.bImageFromDB := ASBDataObj.imagefromdb; //imagefromdb;
      if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
      if tcstr = '' then tcstr := 'd';
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.DbCall.act := ASBDataObj.act;

      dbcall.XMLData := XMLDoc;
      dbcall.Parser.OnLoadAndSave:=ASBDataObj.LoadAndSave;
      dbcall.Parser.OnLoadTrans := LoadTrans;
      dbcall.Parser.OnSaveTrans:=SaveTrans;
      dbcall.Parser.OnEndTrans := EndTrans;
      dbcall.SaveTrans:=SaveTrans;
      dbcall.Parser.OnCopyTransAndSave := CopyTransAndSave;
      dbcall.WorkFlow.TransFullCap := transcap;
      dbcall.Parser.OnSendMail := SendMail;
      // loading data removed for performance change. All data including recordid and idvalues suppose to come from client side in inputXML
      {
      if vartype(xnode.Attributes['recordid']) <> varnull then begin
        x := xnode.Attributes['recordid'];
        ASBCommonObj.structrecord:=vartostr(xnode.Attributes['recordid']);
        if x<>'0' then
        begin
         dbcall.LoadData(strtofloat(x));
         dbm.gf.DoDebug.msg('Time elapsed for loading record = '+inttostr(millisecondsbetween(now(),starttime)));
        end;
      end;
      }
      if (tcstr = 'e') or (tcstr = 'd') then
      begin
        dbm.gf.dodebug.msg('Submitting data to dbcall');
        dbcall.attachments:=vartostr(xmldoc.DocumentElement.attributes['afiles']);
        {
        for i:=0 to xnode.ChildNodes.Count-1 do begin
          f := xnode.ChildNodes[i].NodeName;
          fld:=dbcall.struct.GetField(f);
          if not assigned(fld) then continue;
          v := xnode.ChildNodes[i].Text;
          if pos('<br><br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br><br>',#$D#$A);
          if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D);
          dbcall.SubmitValue(f, v, '', 0, 0, 0);
        end;
        ASBDataObj.RegisterGlobalVars;
        }
        //Assigned to SD.delrowsnode to use in Amend |
        //After RemoveDeletedWebRowsFromStoreDate delrowsnode will be deleted from xmldoc
        if XMLDoc.DocumentElement.ChildNodes.FindNode('delrows') <> nil then
          dbcall.StoreData.delrowsnode := XMLDoc.DocumentElement.ChildNodes.FindNode('delrows').CloneNode(true);

        ASBDataObj.RemoveDeletedWebRowsFromStoreDate;
        ASBDataObj.SubmitClientValuesToSD;
        ASBDataObj.CreateChangedRowsList;
        {As discussed by Sab, DB event actions will be restricting to user defined task only and  it will be handled in uDbcall}
        dbm.gf.dodebug.msg('DB Call Validate and save');
        if SendRecIdsToClient then dbcall.ClearFields := false;
        dbcall.Validate.OnWebSave := True;
        if dbcall.ValidateData = '' then begin
          dbm.StartTransaction(connection.ConnectionName);
          if vartype(xmldoc.DocumentElement.Attributes['dopegapproval']) <> varnull then
          begin
             if lowercase(vartostr(xmldoc.DocumentElement.Attributes['dopegapproval'])) = 't' then
             begin
                dbcall.StoreData.DoPEGApprovalOnSave := true;
                //read taskid from xml
                dbcall.StoreData.sPEGTaskID := vartostr(xmldoc.DocumentElement.Attributes['pegtaskid']);
             end;
          end;
          dbcall.StoreData.SDEvaluateExpr := SDEvaluateExpr;
          dbcall.StoreData.SDRegVarToParser := SDRegVarToParser;
          dbcall.StoreData.Object_ASBDataObj := ASBDataObj;
    		  dbcall.StoreData.ParserObject := dbcall.Parser;

          dbcall.SaveData;
//          if (dbcall.WorkFlowProcessed) and (dbcall.WorkFlow.chkmailcont) then
//          begin
//            prnt := TPrintDocs.create;
//            prnt.Axp := AxProvider;
//            prnt.storedata := dbcall.storedata;
//            prnt.parser := dbcall.parser;
//            prnt.transid := x;
//            dbcall.workflow.OnPrintDocForm := prnt.PrintDocForm;
//            dbcall.workflow.OnPrintPDFaws := prnt.PDFPrint;
//            dbcall.workflow.applname := vartostr(xmldoc.DocumentElement.Attributes['axpapp']);
//            dbcall.WorkFlow.SendMailWorkflow;
//          end;
        end;
        if dbcall.errorstr <> '' then begin
          dbm.gf.dodebug.msg('DB Call error msg : ' +dbcall.ErrorStr );
          raise exception.Create(dbcall.ErrorStr);
        end else begin
          {if dbcall.struct.HasImage then
          begin
            axprovider.savedimages := ',';
            f:=dbcall.storedata.primarytablename+'id';
            v:=floattostr(dbcall.LastSavedId);
            recid := dbcall.GetRecordId(f,v);
            for i:=0 to dbcall.struct.flds.count-1 do begin
              fld := pfld(dbcall.struct.flds[i]);
              tname := '';
              iname := '';
              if (fld.DataType = 'i') and (fld.Tablename<>'') then
              begin
                if trim(dbcall.struct.SchemaName) <> '' then
                  tname := dbcall.struct.SchemaName + '.' + dbcall.transid+trim(fld.FieldName)
                else
                  tname := dbcall.transid+trim(fld.FieldName);
                f := fld.FieldName;
                n := xnode.ChildNodes.FindNode(f) ;
                if assigned(n) then iname := vartostr(n.NodeValue);
                if (not fld.Empty) and (iname = '') then
                  raise exception.Create('Image field '+ f +' can not be left empty.');
                if (tname <> '') and (iname <> '') then
                begin
                  if not axprovider.SaveImage(tname,iname,recid) then
                  begin
                     raise exception.Create('Could not save image...');
                  end;
                end else begin
                  s := 'delete from '+tname+' where recordid ='+FloatToStr(recid);
                  Axprovider.ExecSQL(s,'','',False);
                end;
              end;
            end;
            delete(axprovider.savedimages,1,1);
            if axprovider.savedimages <> '' then
            begin
              i := 1;
              while true do
              begin
                f := dbm.gf.GetnthString(axprovider.savedimages,i);
                if f = '' then break;
                if FileExists(f) then deletefile(f);
                inc(i);
              end;
            end;
          end;}
          if lowercase(dbcall.transid) = 'axglo' then begin
            s := AddToAppVars;
            if s <> '' then
              raise Exception.Create('Error in Global Variables. '+s);
          end;
          dbm.Commit(connection.ConnectionName);
          dbm.gf.dodebug.msg('Transaction commited');
          //Process RMQ Message
          ProcessRMQMessages;
          { checked with Sab . This code need to relook }
          if (dbcall.CallWorkFlowApproval) and (dbcall.WorkFlow.ToBeSaved) then begin
            dbcall.StoreData.SetRecid(dbcall.LastSavedId);
            if (assigned(dbcall.MDMap)) and ((dbcall.MDMap.WorkFlow ='approve') or (dbcall.MDMap.WorkFlow='reject')) then
              dbcall.MDMap.SetInitOld := False
            else begin
              if dbcall.workflow.active then begin
                dbcall.MDMap.WorkFlow := '';
                dbcall.MDMap.SetInitOld := True;
              end;
            end;
            SaveTrans;
          end;

          // to be re-architech to send JSON string - below one is temp.fix as instruced by sab.
          v := '';
          if dbcall.AutoGenString <> '' then begin
          begin
            if splitresult then
               v := dbCall.struct.Caption +  ' Saved (' + dbcall.AutoGenString + ')'
            else v := dbCall.struct.Caption +  ' Saved' + ChangeFormatForSplitresult(dbcall.AutoGenString,'1');
          end;
          end else begin
            if splitresult then
              v := (dbCall.struct.Caption +  ' Saved')
            else v:=(dbCall.struct.Caption +  ' Saved"')
          end;
          if splitresult  then
             v := v + ',' + 'recordid=' + floattostr(dbcall.LastSavedId)
          else v := v + ',' + '"recordid":"' + floattostr(dbcall.LastSavedId);
          if splitresult then ASBDataObj.act.createmsgnode(v)
          else ASBDataObj.act.msgnode := ASBDataObj.act.msgnode+v+',';
          ASBDataObj.act.cmdnode := dbcall.cmdnode;
          if SendRecIdsToClient then CreateRecIdDataNode;
          result := ASBDataObj.GetJSON;
          if splitresult then
          begin
            result := result + '*$*' + '{"result":[{"save": "success"}]}'
          end else
          begin
           ASBDataObj.act.msgnode := ChangeFormatForSplitResult(ASBDataObj.act.msgnode,'2') + '"result":{"save": "success"}}';
           result := ASBDataObj.act.msgnode;
          end;
          if (dbcall.WorkFlowProcessed) and (dbcall.WorkFlow.chkmailcont) then
          begin
            prnt := TPrintDocs.create;
            prnt.Axp := AxProvider;
            prnt.storedata := dbcall.storedata;
            prnt.parser := dbcall.parser;
            prnt.transid := x;
            dbcall.workflow.OnPrintDocForm := prnt.PrintDocForm;
            dbcall.workflow.OnPrintPDFaws := prnt.PDFPrint;
            dbcall.workflow.applname := vartostr(xmldoc.DocumentElement.Attributes['axpapp']);
            dbcall.WorkFlow.SendMailWorkflow;
          end;
        end;
      end else result := '<error>Access denied for saving this transaction</error>';
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on e : Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(dbcall) then
      begin
        if (trim(dbcall.ErrorStr) <> '') and (trim(dbcall.ErrorStr) <> trim(e.Message))  then
           v := e.Message + '  ' + dbcall.ErrorStr
        else v := e.Message;
      end else v := e.Message;
      ASBCommonObj.serviceresult:=copy(v,1,199);
      if assigned(dbcall) then
      begin
        if dbcall.Validate.error_fieldname <> '' then v := v+'" , "errfld" : "' + dbcall.Validate.error_fieldname + ',' + dbcall.Validate.error_row
      end;
      if splitresult then
      begin
        result := CreateErrNode(v,false);
        result := result + '*$*' + '{"result":[{"save": "failure"}]}'
      end else
      begin
        v := '';
        result := '{"error":{"msg":"'+ChangeFormatforSplitResult(v,'3')+'"},"result":{"save": "failure"}}';
      end;
      dbm.gf.dodebug.msg('Error : ' + v);
      if assigned(dbm) then
      begin
        if assigned(dbm.Connection) then
        begin
           if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
           dbm.gf.execActName := 'SaveData';
           if assigned(dbcall) then dbm.update_errorlog(dbcall.transid,v);
           f := dbm.GetAxpertMsg(e.Message);
           if f <> '' then
           begin
              if assigned(dbcall) then
              begin
                if dbcall.Validate.error_fieldname <> '' then f := f+'" , "errfld" : "' + dbcall.Validate.error_fieldname + ',' + dbcall.Validate.error_row
              end;
              if splitresult then
              begin
                result := CreateErrNode(f,false);
                result := result + '*$*' + '{"result":[{"save": "failure"}]}'
              end else
              begin
                result := '{"error":{"msg":"'+ChangeFormatforSplitResult(f,'3')+'"},"result":{"save": "failure"}}';
              end;
           end;
           dbm.gf.execActName := '';
        end;
      end;
    end;
  end;
  dbm.gf.dodebug.msg('Savedata webservice completed');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  try
  if prnt <> nil then
  begin
     prnt.Free;
     prnt := nil;
  end;
  except
  end;
  time_used := (millisecondsbetween(now(),starttime)) + 500; //500ms added for considering closeproject time taken
  time_used := time_used / 1000;
  dbm.gf.dodebug.msg('Client timeout : ' + floattostr(client_timeout) + ' secs');
  dbm.gf.dodebug.msg('Time Used : ' + floattostr(time_used) + ' secs');
  if (client_timeout >0) and (time_used > client_timeout) then
  begin
    try
      dbm.gf.dodebug.msg('Client Time out true, sending message through SignalR');
      GenerateSessionAppKey := False;
      dbm.gf.PushToApiJobs := False;
      ASBCommonObj.AddRecToAxActiveMessagesForLongRunWS(ASBDataObj.dbcall.Parser,'SaveData',dbm.gf.username,
        'SaveDataWS Notification','SaveData',v,ASBDataObj.DbCall.transid);
      dbm.gf.dodebug.msg('Client Time out true, sending message through SignalR done');
    except
    end;
  end;
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.DeleteData(S: String): String;
var x , rem, allowcancel  : String;
    f:extended;
    rNode : ixmlnode;
begin
  result := 'done';
  servicename:='Deleting data';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DeleteData');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to DeleteData Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to DeleteData webservice');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to DeleteData webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing deletedata webservice');
    dbm.gf.dodebug.msg('-------------------------------');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,'');
      dbCall := ASBDataObj.DbCall;
      tcstr := ASBDataObj.CheckForAccess(x);
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.DbCall.act := ASBDataObj.act;
      dbcall.Parser.OnSendMail := SendMail;
      ASBDataObj.SubmitClientValuesToSD;
      ASBCommonObj.structrecord := vartostr(xmldoc.DocumentElement.attributes['recordid']);
      x := vartostr(xmldoc.DocumentElement.attributes['recordid']);
      if x='' then raise exception.create('recordid attribute not specified');
      allowcancel := trim(vartostr(xmldoc.DocumentElement.attributes['allowcancel']));
      if allowcancel = '' then allowcancel := 'true';
      f := dbm.gf.strtofloatz(x);
      x := xmldoc.DocumentElement.attributes['action'];
      dbm.gf.dodebug.msg('Action='+x+' Recordid='+floattostr(f));
      if (dbcall.WorkFlow.Active) and (lowercase(allowcancel) = 'false') then
      begin
        if x = 'cancel' then
          raise exception.create('This transaction cannot be cancelled.')
        else raise exception.create('This transaction cannot be deleted.');
      end;
      if f>0 then begin
        try
         if (tcstr = 'd') or (tcstr = '') then begin
           dbm.StartTransaction(connection.ConnectionName);
           if x='cancel' then begin
             rem := '';
             rNode := xmldoc.DocumentElement.ChildNodes.FindNode('comments');
             if assigned(rnode) then
               rem:= VartoStr(rnode.NodeValue);
             dbcall.CancelTransaction(f,rem)
           end else
             dbcall.DeleteData(f);
           dbm.Commit(connection.ConnectionName);
           if x='cancel' then
             result :=  'Data canceled successfully'
           else
             result :=  'Data deleted successfully';

           //Process RMQ Message
           ProcessRMQMessages;
         end else result := 'Access denied for deleting this transaction';
         s := result;
         ASBDataObj.act.createmsgnode(s);
         ASBDataObj.MakeMemVarNode := false;
         result := ASBDataObj.GetJSON;
        except on e:exception do
          begin
             dbm.RollBack(connection.ConnectionName);
             raise exception.Create(e.Message);
          end;
        end;
      end;
    end;
    dbm.gf.dodebug.msg('Result '+result);
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
      begin
        if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
        dbm.gf.execActName := 'DeleteData';
        if assigned(dbcall) then dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        x := dbm.GetAxpertMsg(e.Message);
        if x <> '' then
        begin
           result := CreateErrNode(x,false);
           dbm.gf.dodebug.msg('Error : ' + x);
        end;
        dbm.gf.execActName := '';
      end;
    end;
  end;
  dbm.gf.dodebug.msg('Executing deletedata webservice');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x:= closeproject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.GetFillGridValues(xml,sxml:string):string;
  var x , fgname , f , s , v,fs,FormAndColsize : String;
      enode,rnode,fgnode,mn : ixmlnode;
      fno,i,j,k : integer;
      fg : pfg;
      flist : TStringList;
begin
  result := '';
  servicename:='Get Fillgrid Values';
  try
    dbm := nil;
    flist := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice GetFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['frameno']) = varnull then
      raise Exception.create('Target DC tag not specified in call to webservice GetFillGridValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetFillGridValues webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := x
    else
    begin
      dbm.gf.dodebug.msg('Received XML ' + xml);
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.SubmitClientValuesToSD;
      fno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['frameno']));
      fgname := '';
      if vartype(xmldoc.DocumentElement.Attributes['fgname']) <> varnull then
         fgname := vartostr(xmldoc.DocumentElement.Attributes['fgname']);
      dbm.gf.dodebug.msg('FG Name : ' + fgname);
      if fgname = '' then raise Exception.Create('FG Name not found...');
      enode := dbcall.struct.XML.DocumentElement;
      xmldoc := nil;
      xmldoc := LoadXMLDataFromWS('<root></root>');
      mn := xmldoc.documentelement;
      rnode := mn.AddChild('response');
      fg := nil;
      for i := 0 to dbcall.struct.fgs.Count - 1 do
      begin
        if (pfg(dbcall.struct.fgs[i]).TargetFrame = fno) and (pfg(dbcall.struct.fgs[i]).fname = fgname)then
        begin
           fg := pfg(dbcall.struct.fgs[i]);
           break;
        end;
      end;
      fgnode := enode.ChildNodes.FindNode(fgname);
      if assigned(fg) and (assigned(fg.q)) then
      begin
        dbm.gf.dodebug.msg('Getting formsize and Colsize');
        FormAndColSize := GetFillgridDefDetails(x,fg.name);
        if fg.MultiSelect then
        begin
           dbm.gf.dodebug.msg('Getting formsize');
           fs :=dbm.gf.GetNthString(FormAndColSize,1,'~');
           if fs = '' then fs := '0,0,0,0';
           dbm.gf.dodebug.msg('Formsize : ' + fs);
           DbCall.Validate.DynamicSQL := fg.q.CDS.CommandText;
           DbCall.Validate.QueryOpen(fg.q,1);
           rnode.Attributes['multiselet'] := 'true';
           rnode := mn.ChildNodes.FindNode('response') ;
           WriteResultForGrid(fg.q,rnode,fgnode,fs,dbm.gf.GetNthString(FormAndColSize,2,'~'));
        end else
        begin
          flist := TStringList.Create;
          DbCall.Validate.DynamicSQL := fg.q.CDS.CommandText;
          DbCall.Validate.QueryOpen(fg.q,1);
          if fg.q.CDS.RecordCount > 0 then
            rnode.Attributes['multiselet'] := 'false';
          rnode := mn.ChildNodes.FindNode('response');
          WriteResultForGrid(fg.q,rnode,fgnode,'',dbm.gf.GetNthString(FormAndColSize,2,'~'));
          while not fg.q.cds.eof do begin
            for i:=0 to dbcall.struct.flds.count-1 do begin
              if pfld(dbcall.struct.flds[i]).FrameNo <> fno then continue;
              f := pfld(dbcall.struct.flds[i]).fieldname;
              s := fg.Map.Values[f];
              if s<>'' then begin
                v := fg.q.cds.fieldbyname(s).asstring;
                dbcall.SubmitValue(f, v, '', 0, 0, 0);
                ASBDataObj.GetDepentendFldList(f,flist);
                j := flist.IndexOf(f);
                if j > -1 then flist.Delete(j);
              end;
            end;
            fg.q.cds.next;
          end;

          for i := 0 to flist.Count - 1 do
          begin
            s := flist.Strings[i];
            fld:=dbcall.struct.GetField(s);
            if not assigned(fld) then continue;
            if fld.txtSelection = true then
            begin
              if fld.Exprn > -1 then
              begin
                If DbCall.Parser.EvalPrepared(fld.Exprn) Then
                  v := DbCall.Parser.Value;
                DbCall.StoreData.SubmitValue(fld.FieldName, 1, v, '', 0, 0, 0);
                DbCall.Parser.RegisterVar(fld.FieldName,fld.DataType[1],v);
                MakeResponse(v,mn,enode);
              end else MakeResponse('',mn,enode);
              continue;
            end;
            if not fld.AsGrid then
            begin
               v := DbCall.Validate.RefreshField(fld,1);
               MakeResponse(v,mn,enode);
            end else if (fld.AsGrid) and (fno = fld.FrameNo) then
            begin
               v := DbCall.Validate.RefreshField(fld,1) ;
               MakeResponse(v,mn,enode);
            end else if fld.AsGrid then
            begin
               j := dbcall.StoreData.RowCount(fld.FrameNo);
               for k := 1 to j do
               begin
                 dbCall.validate.RegRow(fld.FrameNo,j);
                 v := DbCall.Validate.RefreshField(fld,k) ;
                 MakeResponse(v,mn,enode);
               end;
            end;
          end;
        end;
      end;
    end;
    result := xmldoc.DocumentElement.XML;
    dbm.gf.dodebug.msg('Result : ' + result);
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,true);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  if assigned(flist) then FreeAndNil(flist);
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(xmldoc,'');
end;

function TASBTStructObj.DoFillGridValues(xml,sxml:string):string;
var fldname,s,x,v,fgname,formload_fldlist:String;
    enode,n : ixmlnode;
    fno,i,j,rcount,prowcount,k:integer;
    fg : pfg;
    fm : pFrm;
begin
  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := DoFillGridValuesNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;
  servicename:='Do FillGrid';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['frameno']) = varnull then
      raise Exception.create('Target DC tag not specified in call to webservice GetFillGridValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing DoFillGridValues webservice');
    x := ASBCommonObj.ValidateSession ;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XML ' + xml);
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      fno:=0;
      if vartype(xmldoc.DocumentElement.Attributes['fgname']) <> varnull then
         fgname := vartostr(xmldoc.DocumentElement.Attributes['fgname']);
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
         fno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['frameno']));
      dbm.gf.dodebug.msg('Fillgrid Name : ' + fgname);
      xnode := xmldoc.DocumentElement;
      enode := dbcall.struct.XML.DocumentElement;
      fg:=nil;
      for i:=0 to dbcall.struct.fgs.count-1 do begin
        if (pfg(dbcall.struct.fgs[i]).TargetFrame = fno) and (pfg(dbcall.struct.fgs[i]).fname = fgname)then
        begin
          fg := pfg(dbcall.struct.fgs[i]);
          break;
        end;
      end;
      xnode := xnode.ChildNodes.FindNode('GridList');
      if xnode.ChildNodes.Count > 0 then
      begin
        if assigned(fg) then
        begin
          dbm.gf.dodebug.msg('fillgrid assigned');
          if fg.AddRows <> 3 then
          begin
            if fg.AddRows = 2 then dbcall.InitGrid(fg.TargetFrame)
            else begin
              if fg.firmbind then dbcall.InitGrid(fg.TargetFrame)
              else begin
                j := dbCall.StoreData.GetRowCount(fg.TargetFrame);
                dbm.gf.DoDebug.msg('Row count : ' + inttostr(j));
                if (j = 1) and (dbcall.Validate.GetRowValidity(dbCall.StoreData.GetRowCount(fg.TargetFrame),1) = 0) then exit
                else if (j = 1) then dbcall.InitGrid(fg.TargetFrame);
              end;
            end;
          end;
          dbCall.DoMultiSelectFillGrid(fg,xnode);
          dbm.gf.dodebug.msg('Multiselect Row values submitted and depedents refreshed');
          prowcount := dbcall.StoreData.RowCount(fno);
          if prowcount > 0 then
          begin
             fm:=dbcall.Struct.GetFrame(fno);
             fm.HasDataRows := True;
          end;
          AsbDataObj.GridToJSON(fno, true);
          AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
          PopGridJSON(fno,prowcount);
          AsbDataObj.EndDataJSON;
          Result:=AsbDataObj.GetJSON;
          dbm.gf.dodebug.msg('Result : ' + result);
        end;
      end else begin
        AsbDataObj.jsonstr:='';
        ExecFillGrid(fno, fgFromButtonClick,fgname);
        prowcount := dbcall.StoreData.RowCount(fno);
        if prowcount > 0 then
        begin
           fm:=dbcall.Struct.GetFrame(fno);
           fm.HasDataRows := True;
        end;
        AsbDataObj.GridToJSON(fno, true);
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
        PopGridJSON(fno,prowcount);
        AsbDataObj.EndDataJSON;
        Result:=AsbDataObj.GetJSON;
        dbm.gf.dodebug.msg('Result : ' + result);
      end;
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

procedure TASBTStructObj.PopGridJSON(pframeno,prowcount : integer);
  var i,j,rcount : integer;
begin
  for i := 0 to dbcall.struct.popgrids.count-1 do begin
    if pPopGrid(dbcall.Struct.PopGrids[i]).ParentFrameNo <> pframeno then continue;
    j := pPopGrid(dbcall.Struct.PopGrids[i]).FrameNo;
    rcount := dbcall.StoreData.RowCount(j) ;
    if pFrm(dbcall.struct.frames[j-1]).HasDataRows then
    begin
      asbDataObj.PopDCToJSON(j, RCount);
      for j := 1 to prowcount do
      begin
        AsbDataObj.PopGridToJSON(pPopGrid(dbcall.Struct.PopGrids[i]), j, true);
      end;
    end
  end;
end;


function TASBTStructObj.GetFieldChoices(s,sxml:string) :widestring;
var x,val,sqltext,stable,sfield,dlist,sqlfld,map,sqcol,tmpsql,v, SearchCond, val2 , sqlerror , orgSql :String;
    k,i:integer;
    q:TXDS;
    n,rnode : IXMLNode;
    paramok,dynamicfilter : boolean;
    sqlcol : AnsiString;
begin
  servicename:='Get Field Choices';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetFieldChoices');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to GetFieldChoices WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to GetFieldChoices WebService');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to GetFieldChoices WebService');
    if vartype(xmldoc.DocumentElement.Attributes['sqlfield']) = varnull then
      raise Exception.create('field tag not specified in call to GetFieldChoices WebService');
    if vartype(xmldoc.DocumentElement.Attributes['value']) = varnull then
      raise Exception.create('value tag not specified in call to GetFieldChoices WebService');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    sqlcol := '';
    if xmldoc.DocumentElement.HasAttribute('sqlcolumn') then
       sqlcol := vartostr(xmldoc.DocumentElement.Attributes['sqlcolumn']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetFieldChoices webservice');
    dbm.gf.dodebug.msg('Received XMl ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
      result := x
    else
    begin
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        //---
        x:= xmldoc.DocumentElement.Attributes['transid'];
        ASBDataObj.CreateAndSetDbCall(x,sxml);
        dbCall := ASBDataObj.DbCall;
        ASBDataObj.SubmitClientValuesToSD;
        dbcall.Validate.loading:=True;
        x := trim(xmldoc.DocumentElement.Attributes['field']);
        sqlfld := trim(xmldoc.DocumentElement.Attributes['sqlfield']);
        val := xmldoc.DocumentElement.Attributes['value'];
        SearchCond := ''; val2 := '';
        if vartype(xmldoc.DocumentElement.Attributes['cond']) <> varnull then
          SearchCond := lowercase(trim(xmldoc.DocumentElement.Attributes['cond']));
        if vartype(xmldoc.DocumentElement.Attributes['value1']) <> varnull then
          val2 := xmldoc.DocumentElement.Attributes['value1'];
        fld:=dbcall.struct.GetField(x);
        q:=fld.QSelect;
        if assigned(q) then
        begin
          sqltext := q.CDS.CommandText;
          orgSql := q.CDS.CommandText;
          dbm.gf.dodebug.msg('Sql Text : ' + sqltext);
          if (pos('{dynamicfilter',lowercase(q.cds.CommandText)) > 0)  then
          begin
             stable := '';
             sfield := '';
             dynamicfilter := true;
          end else if (pos(' dual',lowercase(q.cds.CommandText)) = 0)  then
          begin
            dynamicfilter := false;
            sfield := fld.SourceField ;
            if sfield <> '' then
            begin
              dbm.gf.dodebug.msg('Source Field : ' + sfield);
              stable := '.' + lowercase(sfield);
              s := lowercase(sqltext);
              i := pos(stable,s);
              if i = 0 then
              begin
                stable := lowercase(sfield) + ',';
                s := lowercase(sqltext);
                i := pos(stable,s);
              end else i := i+1;
              stable := '';
              if copy(sqltext,i-1,1) = '.' then
              begin
                s := '';
                for k := i-2 downto 0 do
                begin
                   if (sqltext[k] = ' ') or (sqltext[k] = ',') then break;
                   s := sqltext[k] + s;
                end;
                if trim(s) <> '' then stable := trim(s);
              end;
            end else
            begin
               s := lowercase(sqltext);
               i := pos(',',s);
               if i = 0 then i := pos('from',s);
               s := copy(sqltext,1,i-1);
               i := pos('select',lowercase(s));
               sfield := trim(copy(s,i+6,length(s)));
               i := pos('as ',lowercase(sfield));
               if i > 0 then delete(sfield,1,i+1);
               sfield := trim(sfield);
               dbm.gf.dodebug.msg('Source Field : ' + sfield);
               if pos('where',lowercase(sqltext)) > 0  then
               begin
                i := pos('.',sfield);
                if i > 0 then
                begin
                   stable := copy(sfield,1,i-1);
                   sfield := '';
                end;
               end;
            end;
            if stable = '' then
            begin
              stable := fld.Sourcetable;
              i := pos('.',stable);
              if i > 0 then stable := copy(stable,i+1,length(stable))
              else begin
                s := lowercase(sqltext);
                i := pos('from',s);
                delete(s,1,i+4);
                s := trim(s);
                i := pos('where',s);
                if i = 0 then i := pos('order',s);
                if i = 0 then i := pos('union',s);
                if i = 0 then
                begin
                  i := pos(stable,s);
                  if i > 0 then
                  begin
                    delete(s,1,i+length(stable));
                    if s <> '' then stable := trim(s);
                  end else stable := trim(s);
                end else
                begin
                  delete(s,i,length(s));
                  i := pos(stable,s);
                  if i > 0 then
                  begin
                    delete(s,1,i+length(stable));
                    if s <> '' then stable := trim(s);
                  end else stable := trim(s);
                end;
              end;
            end;
          end else stable := 'dual';
          dbm.gf.dodebug.msg('Source Table : ' + stable);
          if (stable <> '') and (sfield <> '')then
          begin
            if SearchCond <> '' then
            begin
              if SearchCond = 'starts with' then val :=  lowercase(val) + '%'
              else if SearchCond = 'ends with' then val :=  '%' + lowercase(val)
              else if SearchCond = 'contains' then val :=  '%' + lowercase(val) + '%'
              else if SearchCond = 'between' then val :=  quotedstr(lowercase(val)) +' and ' + quotedstr(lowercase(val2))
              else val :=  lowercase(val);
              if (SearchCond = 'starts with') or (SearchCond = 'ends with') or (SearchCond = 'contains') then
                val := 'lower('+stable + '.' + sfield + ') like ' + quotedstr(val) + ' and '
              else if (SearchCond = 'between') then
                val := 'lower('+stable + '.' + sfield + ') between ' + val + ' and '
              else val := 'lower('+stable + '.' + sfield + ') '+ SearchCond + ' '+ quotedstr(val) + ' and '
            end
            else
            begin
              val := '%' + lowercase(val) + '%';
              val := 'lower('+stable + '.' + sfield + ') like ' + quotedstr(val) + ' and ';
            end;
            dbm.gf.dodebug.msg('Changing SQL Text');
            sqltext := InsertCondition(sqltext,val);
            dbm.gf.dodebug.msg('Changed Sql Text : ' + sqltext);
            //-- for pagination  web
            if dbm.gf.pagination_pageno = 1 then
            begin
               s := dbm.ChangeSqlForPagination(sqlText,true);
               q.CDS.CommandText := s;
               paramok := assignparams(q);
               if paramok = false then raise exception.create('Please provide all required information');
               sqlerror := '';
               try
                 q.open;
                 dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                 q.close;
               except on e:exception do
                 sqlerror := 'yes';
               end;
            end;
            if (dbm.gf.pagination_totalrows = 0) and (dbm.gf.pagination_pageno = 1)then
            begin
              if sqlerror = 'yes' then
              begin
                q.CDS.CommandText := orgSql;
                paramok := assignparams(q);
                if paramok = false then raise exception.create('Please provide all required information');
                q.open;
                xmldoc.DocumentElement.Attributes['pagesize'] := '0';
                rnode := xmldoc.documentelement.AddChild('response');
                rnode.Attributes['totalrows'] := inttostr(q.cds.RecordCount);
              end else
              begin
                rnode := xmldoc.documentelement.AddChild('response');
                rnode.Attributes['totalrows'] := '0';
              end;
            end
            else begin
              sqlText := dbm.ChangeSqlForPagination(sqlText,false);
              dbm.gf.dodebug.msg('Sql after pagination change  :' + sqlText);
              q.CDS.CommandText := sqltext ;
              paramok := assignparams(q);
              if paramok = false then raise exception.create('Please provide all required information');
              q.open;
              rnode := xmldoc.documentelement.AddChild('response');
              if dbm.gf.pagination_totalrows >= 0 then rnode.Attributes['totalrows'] := inttostr(dbm.gf.pagination_totalrows);
            end;
          end else if stable = 'dual' then
          begin
              paramok := assignparams(q);
              if paramok = false then raise exception.create('Please provide all required information');
              q.open;
              xmldoc.DocumentElement.Attributes['pagesize'] := '0';
              rnode := xmldoc.documentelement.AddChild('response');
              rnode.Attributes['totalrows'] := inttostr(q.cds.RecordCount);
          end else begin
            v := vartostr(xmldoc.DocumentElement.Attributes['value']);
            GetPickListResult(fld,sqlfld,v, SearchCond, val2);
            rnode := xmldoc.documentelement.AddChild('response');
            if dbm.gf.pagination_totalrows >= 0 then rnode.Attributes['totalrows'] := inttostr(dbm.gf.pagination_totalrows);
          end;
          // to create map fields for coloumn information from clent
          if pos('{dynamicfilter',lowercase(tmpsql)) = 0  then
          begin
            n :=dbcall.struct.XML.DocumentElement.ChildNodes.FindNode(x);
            if n <> nil then n := n.ChildNodes.FindNode('a7');
            if (n <> nil) and (n.ChildNodes.Count > 0) then
            begin
              n := n.ChildNodes.FindNode('a26');
              if (vartostr(n.Attributes['stype']) = 'tstruct') or (vartostr(n.Attributes['stype']) = 'sql') then
              begin
                 map := vartostr(n.NodeValue);
                 rnode.Attributes['colmap'] := map;
              end;
            end;
          end;
          if lowercase(sqlcol) = 'true' then
             WriteSQLResultForGetFieldValue(q,rnode,dynamicfilter,map)
          else WriteResultForGetFieldValue(q,rnode,dynamicfilter,map);
          rnode :=xmldoc.DocumentElement;
          if fld.SourceKey then rnode.Attributes['idcol'] := 'yes'
          else rnode.Attributes['idcol'] := '';
          // creating sqlfld= depfld list for mapping purpose at client side
          map :=  '';
          n :=dbcall.struct.XML.DocumentElement.ChildNodes.FindNode(x);
          if n <> nil then n := n.ChildNodes.FindNode('a58');
          {
          if (n <> nil) and (n.ChildNodes.Count > 0) then
          begin
            n := n.ChildNodes.FindNode('f');
            if assigned(n) then dlist := dlist + vartostr(n.NodeValue);
          end;
          }
          if (n <> nil) then
          begin
            sfield := vartostr(n.NodeValue);
            dlist := '';
            if sfield <> '' then
            begin
              i := 1;
              while true do
              begin
                s := dbm.gf.GetnthString(sfield,i);
                if s = '' then break;
                if copy(s,1,1) = 'f' then
                begin
                  delete(s,1,1);
                  dlist := dlist + s + ',' ;
                end;
                i := i + 1;
              end;
              if dlist <> '' then delete(dlist,length(dlist),1);
            end;
          end;
          if dlist <> '' then
          begin
            if pos(',',dlist) > 0 then
            begin
              i := 1;
              while true do
              begin
                s := dbm.gf.GetnthString(dlist,i);
                if s = '' then
                begin
                   i := length(map);
                   s := copy(map,i,1);
                   if s = ',' then delete(map,i,1);
                   break;
                end;
                i := i + 1;
                fld:=dbcall.struct.GetField(s);
                sqlfld := fld.SourceField;
                map := map + sqlfld + '=' + s + ','
              end;
            end else
            begin
                fld:=dbcall.struct.GetField(dlist);
                sqlfld := fld.SourceField;
                map := sqlfld + '=' + dlist ;
            end;
          end;
        end;
        k := 0;
        rnode :=xmldoc.DocumentElement;
        rnode.Attributes['map'] := map;
        while k<rnode.ChildNodes.count do
        begin
          if rnode.childnodes[k].NodeName='response' then break;
          rnode.ChildNodes.Delete(k);
        end;
        result := xmldoc.DocumentElement.XML;
        dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      close_err := copy(e.message,1,199);
      if assigned(ASBCommonObj) then ASBCommonObj.serviceresult:=close_err;
      rnode := xmldoc.documentelement.AddChild('response');
      k := 0;
      rnode :=xmldoc.DocumentElement;
      while k<rnode.ChildNodes.count do begin
        if rnode.childnodes[k].NodeName='response' then break;
        rnode.ChildNodes.Delete(k);
      end;
      result := xmldoc.DocumentElement.XML;
      if assigned(dbm) then
      begin
        dbm.gf.dodebug.msg('Result : ' + result);
      end;
    end;
  end;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(xmldoc,'');
end;


procedure TASBTStructObj.GetPickListResult(fld : pFld ; sqlfld,val : String; SearchCond:string=''; SearchVal2 : string = '');
var  i, k : integer;
     SQLString : String;
     NonDynamicFilterSQL : Boolean;
begin
  dbm.gf.dodebug.msg('Geting PickList SQL result');
  if trim(sqlfld) = '' then
  begin
    //k := 0;
    //if fld.SourceKey then k := 1;
    k:=1;
  end else begin
    for i := 0 to fld.pickcaptions.Count - 1 do
    begin
      if lowercase(sqlfld) = trim(lowercase(fld.pickcaptions[i])) then
      begin
        k := i+1;
        break;
      end;
    end;
  end;
  Fld.QSelect.Close;
  NonDynamicFilterSQL := false;
  SQLString := Fld.SQL.Text;
  if pos('dynamicfilter',lowercase(Fld.SQL.Text)) = 0 then
  begin
//    NonDynamicFilterSQL := true;
  end
  else if (assigned(Fld.pickfields)) and (Fld.pickfields.count > 0) then
    SQLString := ChangeSQL(Fld.SQL.Text, Val,Fld.pickfields,k, SearchCond, SearchVal2);
  Fld.QSelect.CDS.CommandText:=SQLString;
//  dbcall.Validate.DynamicSQL := SQLString;
  if Fld.DynamicParams then dbcall.Validate.DynamicSQL:=Fld.QSelect.CDS.CommandText else dbcall.Validate.DynamicSQL:='';
  if (FastDataFlag) then
  begin
//    if (not fld.txtSelection) and (not NonDynamicFilterSQL) then
//      NonDynamicFilterSQL := true;
    dbCall.Validate.NonDynamicFilterSQL := NonDynamicFilterSQL;
  end;
  dbCall.Validate.QueryOpen(Fld.QSelect,1,'0',Val);
  dbm.gf.dodebug.msg('Geting PickList SQL result over');
end;

function TASBTStructObj.ChangeSQL(SqlText,SearchValue:String;SearchFldList:TStringList;SearchIdx:Integer; SearchCond,SearchValue2 : string):String;
var l, i, j, k:integer;
    replacewith, s, SearchField,tail:String;
begin
   result := sqlText;
   if pos('dynamicfilter',lowercase(sqlText)) = 0 then exit;
   l := length(sqltext);
   if (not FastDataFlag) and (SearchCond = '') then
   begin
     if dbm.gf.PickListMode = plmStartWith then
       SearchValue := searchvalue+'%'
     else
       SearchValue := '%'+searchvalue+'%';
   end
   else if FastDataFlag then
   begin
      if SearchCond = 'starts with' then SearchValue :=  lowercase(SearchValue) + '%'
      else if SearchCond = 'ends with' then SearchValue :=  '%' + lowercase(SearchValue)
      else if SearchCond = 'contains' then SearchValue :=  '%' + lowercase(SearchValue) + '%'
      else if SearchCond = 'between' then SearchValue :=  quotedstr(lowercase(SearchValue)) +' and ' + quotedstr(lowercase(SearchValue2))
      else if SearchCond <> '' then SearchValue :=  lowercase(SearchValue)
      else SearchValue :=  '%' + lowercase(SearchValue) + '%';
   end;

   dbCall.Parser.RegisterVar('axp_dynamicfilter','c',SearchValue);

   k := -1;
   while true do begin
       i := pos('{dynamicfilter',lowercase(sqlText));
       if i=0 then break;
       tail:=copy(sqltext,i,length(sqltext));                              // changed due to picklist not working for companyparams..
       j := pos('}',lowercase(tail));
       inc(k);
       SearchField := dbm.gf.GetNthString(SearchFldList[k],SearchIdx);
       if (not FastDataFlag) and (SearchCond = '') then
         replacewith := dbm.gf.sqllower+'('+searchfield+')' + ' like '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'  //+ SearchValue;
       else if FastDataFlag then
       begin
         if (SearchCond = 'starts with') or (SearchCond = 'ends with') or (SearchCond = 'contains') or (SearchCond = '') then
           replacewith := dbm.gf.sqllower+'('+searchfield+')' + ' like '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'
         else if (SearchCond = 'between') then
           replacewith := dbm.gf.sqllower+'('+searchfield+')' + ' between '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'
         else
           replacewith := dbm.gf.sqllower+'('+searchfield+') ' + SearchCond + ' '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'
       end;

       if pos('where', lowercase(SQLText)) > 0 then
         s:=' and '+ replacewith
       else
         s:=' where ' + replacewith;
       delete(sqltext, i, j);
       insert(S, SQLText, i);
   end;
   result := sqlText;
end;

function TASBTStructObj.MakeProperValue(Str: String): String;
var
  i,j: Integer;
begin
  i:=1;
  j:=0;
  while i<=Length(Str) do
  begin
    if Str[i] = '"' then j := j + 1;
    inc(i);
  end;
  if j mod 2 > 0 then
  begin
    i := Length(Str);
    while i >= 0 do
    begin
      if Str[i] = '"' then break;
      if Str[i] = ':'  then Insert(':',Str,i);
      dec(i);
    end;
  end;
  Result:=Str;
end;

function TASBTStructObj.AddGridRowValues(xml,sxml:string):string;
var fldname,x,formload_fldlist  :String;
    fno,rno : integer;
begin

  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := AddGridRowValuesNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;

  servicename:='Add Grid Row Values';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    rno:=1;
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in AddGridRowValues');
      rno:=rno+1;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice AddGridRowValues');
      rno:=rno+1;
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice AddGridRowValues');
      rno:=rno+1;
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice AddGridRowValues');
      rno:=rno+1;
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
      rno:=rno+1;
    {if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);}
    x := xmldoc.documentelement.attributes['axpapp'];
      rno:=rno+1;
    ConnectToProject(x);
      rno:=rno+1;
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing AddGridRowValues webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XMl ' + xml);
      dbm.gf.dodebug.msg('Received XMl ' + sxml);
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      fno:=0;rno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
         fno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
      if vartostr(xmldoc.DocumentElement.Attributes['rowno']) <> '' then
         rno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['rowno']));
      AddGridRow(fno, rno);
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message+' '+inttostr(rno),false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.DeleteGridRowValues(xml,sxml:string):string;
var fldname,x,activerowdep,s,formload_fldlist :String;
    fno,rno,k :integer;
begin
  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := DeleteGridRowValuesNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;
  servicename:='Delete GridRow Values';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DeleteGridRowValues') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DeleteGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice DeleteGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DeleteGridRowValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing DeleteGridRowValues webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XMl ' + xml);
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall(x,'');
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      fldname := xmldoc.DocumentElement.Attributes['field'];
      fno:=0;rno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
         fno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['frameno']));
      if vartostr(xmldoc.DocumentElement.Attributes['rowno']) <> '' then
         rno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['rowno']));
      dbCall.RefreshGridDependents(dbcall.Struct.GetFrame(fno));
      AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
      AsbDataObj.EndDataJSON;
      Result:=AsbDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.LoadDCCombos(xml,sxml:string):string;
var dcname,x,s,formload_fldlist:String;
    fno : integer;
    stime:TDateTime;
    nofill : boolean;
begin
  result := '';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := LoadDCCombosNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;
  servicename:='Load DC Combo';
  try
    stime:=now;
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in LoadDCCombos') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice LoadDCCombos');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice LoadDCCombos');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice LoadDCCombos');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    nofill := false;
    if vartype(xmldoc.DocumentElement.Attributes['nofill']) <> varnull then
    begin
       if vartostr(xmldoc.DocumentElement.Attributes['nofill']) = 'T' then
          nofill := true;
    end;
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing LoadDCCombos webservice');
    dbm.gf.dodebug.msg('---------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      dcname := xmldoc.DocumentElement.Attributes['dcname'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      s := dcname;
      delete(s,1,2);
      if s <> '' then fno := strtoint(s);
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      LoadDCJSON(fno,nofill);
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
      dbm.gf.dodebug.msg('Time taken by LoadDCJSON - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing LoadDCCombos webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.RefreshDC(xml,sxml:string):string;
var dcname,x,s:string;
    fno : integer;
    stime:TDateTime;
begin
  result := '';
  servicename:='Load DC';
  try
    stime:=now;
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in RefreshDC') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice RefreshDC');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice RefreshDC');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice RefreshDC');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing RefreshDC webservice');
    dbm.gf.dodebug.msg('---------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      dcname := xmldoc.DocumentElement.Attributes['dcname'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      s := dcname;
      delete(s,1,2);
      if s <> '' then fno := strtoint(s);
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      ASBDataObj.RefreshDC(fno);
      ASBDataObj.EndDataJSON;
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
      dbm.gf.dodebug.msg('Time taken by RefreshDC - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing LoadDCCombos webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.GetSubGridDropDown(xml,sxml:string):string;
var dcname,x,s:string;
    fno : integer;
    stime:TDateTime;
begin
  result := '';
  servicename:='Load DC Combo';
  try
    stime:=now;
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetSubGridDropDown') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetSubGridDropDown');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice GetSubGridDropDown');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice GetSubGridDropDown');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetSubGridDropDown webservice');
    dbm.gf.dodebug.msg('--------------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      dcname := xmldoc.DocumentElement.Attributes['dcname'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      if vartostr(xmldoc.DocumentElement.Attributes['activerow']) <> '' then
      begin
         dbCall.Parser.RegisterVar('activerow', 'n', vartostr(xmldoc.DocumentElement.Attributes['activerow']));
      end;
      if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
      begin
         if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
         dbCall.Parser.RegisterVar('activeprow', 'n', vartostr(xmldoc.DocumentElement.Attributes['prow']));
      end;
      s := dcname;
      delete(s,1,2);
      if s <> '' then fno := strtoint(s);
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      LoadSubGridJSON(fno);
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
      dbm.gf.dodebug.msg('Time taken by GetSubGridDropDown - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing GetSubGridDropDown webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.SetAtachments(S , sxml : String): String;
var x,sname,recordid,filename,SchemaName : String;
    accxml : ixmldocument;
    anode : ixmlnode;
begin
  servicename:='Set attachments';
  try
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in SetAtachments') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to SetAttachments Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to SetAttachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to SetAttachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['filename']) = varnull then
      raise Exception.create('filename tag not specified in call to SetAttachments webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing SetAttachment webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      sname := vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recordid := vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      filename := vartostr(xmldoc.DocumentElement.Attributes['filename']);
      ASBDataObj.CreateActionObj(sname,'tstruct');
      act := ASBDataObj.act;
//      accxml :=  axprovider.GetStructure('tstructs',sname,'','');
      accxml := LoadXMLDataFromWS(sxml);
      anode := accxml.documentelement.ChildNodes[0] ;
      SchemaName := Trim(vartostr(anode.ChildValues['a24']));
      if SchemaName <> '' then sname := SchemaName + '.' + sname;
      result := Axprovider.SetAttachments(sname,recordid,filename);
      act.createmsgnode(result);
      ASBDataObj.CreateMessageNode;
      Result := ASBDataObj.jsonstr;
      dbm.gf.dodebug.msg('Result : ' + Result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
         if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
    end;
  end;
  accxml := nil;anode := nil;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.ViewAtachments(S , sxml: String): String;
var x,sname,recordid,filename,SchemaName : String;
    accxml : ixmldocument;
    anode : ixmlnode;
begin
  servicename:='View attachments';
  try
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in ViewAtachments') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to ViewAttachments Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to ViewAttachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to ViewAttachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['filename']) = varnull then
      raise Exception.create('filename tag not specified in call to ViewAttachments webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing ViewAttachment webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      sname := vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recordid := vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      filename := vartostr(xmldoc.DocumentElement.Attributes['filename']);
      ASBDataObj.CreateActionObj(sname,'tstruct');
      act := ASBDataObj.act;
//      accxml :=  axprovider.GetStructure('tstructs',sname,'','');
//      accxml := LoadXMLDataFromWS(sxml);
//      anode := accxml.documentelement.ChildNodes[0] ;
//      SchemaName := Trim(vartostr(anode.ChildValues['a24']));
//      if SchemaName <> '' then sname := SchemaName + '.' + sname;
      result := Axprovider.ViewAttachments(dbm.gf.sessionid,sname,recordid,filename);
      act.createcmdnode('openfile',result);
      act.cmdnode := act.cmdnode + '},';
      ASBDataObj.CreateCommandNode;
      Result := ASBDataObj.jsonstr;
      dbm.gf.dodebug.msg('Result : ' + Result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if (assigned(dbm)) and (dbm.InTransaction) then
        dbm.RollBack(connection.ConnectionName);
    end;
  end;
  accxml := nil;anode := nil;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.RemoveAtachments(S,sxml: String): String;
var x,sname,recordid,filename,SchemaName : String;
    accxml : ixmldocument;
    anode : ixmlnode;
begin
  servicename:='Remove Attachments';
  try
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in RemoveAtachments') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to RemoveAtachments Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to RemoveAtachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to RemoveAtachments webservice');
    if vartype(xmldoc.DocumentElement.Attributes['filename']) = varnull then
      raise Exception.create('filename tag not specified in call to RemoveAtachments webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing SetAttachment webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      sname := vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recordid := vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      filename := vartostr(xmldoc.DocumentElement.Attributes['filename']);
      ASBDataObj.CreateActionObj(sname,'tstruct');
      act := ASBDataObj.act;
//      accxml :=  axprovider.GetStructure('tstructs',sname,'','');
      accxml := LoadXMLDataFromWS(sxml);
      anode := accxml.documentelement.ChildNodes[0] ;
      SchemaName := Trim(vartostr(anode.ChildValues['a24']));
      if SchemaName <> '' then sname := SchemaName + '.' + sname;
      result := Axprovider.RemoveAttachments(sname,recordid,filename);
      act.createmsgnode(result);
      ASBDataObj.CreateMessageNode;
      Result := ASBDataObj.jsonstr;
      dbm.gf.dodebug.msg('Result : ' + Result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if (assigned(dbm)) and (dbm.InTransaction) then
        dbm.RollBack(connection.ConnectionName);
    end;
  end;
  accxml := nil;anode := nil;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function  TASBTStructObj.GetHistoryData(S , sxml : string) : widestring;
var  tstrlst : TStringlist;
     w , SchemaName,p,x,transid,recordid : string;
     HXMLData,xd : IXMLDocument;
     n : IXMLNode;
     stm : TStringStream;
     track_chng : boolean;
begin
  servicename:='Get History data';
  tstrlst := nil;
  try
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in LoadData') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in to GetHistoryData webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetHistoryData webservice');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else
    begin
      SchemaName := '';
      transid := vartostr(xmldoc.documentelement.attributes['transid']);
      recordid := vartostr(xmldoc.DocumentElement.attributes['recordid']);
//      xd :=  axprovider.GetStructure('tstructs',transid,'','');
      xd := LoadXMLDataFromWS(sxml);
      n := xd.documentelement.ChildNodes[0] ;
      SchemaName := Trim(vartostr(n.ChildValues['a24']));
      n := n.ChildNodes['a6'];
      track_chng := lowercase(vartostr(n.Attributes['tchng']))='y';    //ch1
      if SchemaName <> '' then transid := SchemaName + '.' + transid;
      w := 'recordid= :recid';
      p := recordid;
      tstrlst := TStringlist.Create;
      if track_chng then
         CreateNewFormatHistoryData(tstrlst,transid,recordid)
      else begin
        stm := TStringStream.Create('');
        dbm.ReadMemo('ChangedValue',transid+'History',w,p,'n',stm);
        if stm.Size=0 then tstrlst.Add('<end>')
        else begin
          tstrlst.Text := stm.DataString;
          tstrlst.Add('<end>');
        end;
        FreeAndNil(stm);
      end;
      dbm.gf.dodebug.msg('History Text : ' + tstrlst.Text);
      HXMLData := CreateHistoryData(tstrlst,xd);
      result := HXMLData.DocumentElement.XML;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,true);
      dbm.gf.DoDebug.msg('Error while evaluating - '+result);
    end;
  end;
  FreeAndNil(tstrlst);
  xd := nil;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(HXMLData,'');
end;

procedure TASBTStructObj.CreateNewFormatHistoryData(history_slist : TStringList; transid,recordid : string );
var
 x:TXDS;
 field_name,old_value,new_value,username,saved_date,date_string,row_num:string;
 delete_flg,field_string,prev_saved_date : string;
 new_rec : boolean;
begin
  new_rec := false; prev_saved_date := '';
  x := dbm.GetXDS(nil);
  x.buffered := true;
  x.CDS.CommandText := 'select * from '+transid+'history where recordid='+(quotedstr(recordid))+ ' order by modifieddate desc';
  x.open;
  while not x.CDS.Eof do begin
    if lowercase(trim(x.CDS.FieldByName('newtrans').AsString)) = 't' then begin
      new_rec := true;
      history_slist.Add('<d>');
      username := lowercase(x.CDS.FieldByName('username').AsString);
      saved_date    := lowercase(x.CDS.FieldByName('modifieddate').AsString);
      date_string   := saved_date + ','+username;
      history_slist.Add(date_string);
      history_slist.Add('<nt>');
      x.CDS.Next;
      continue;
    end;
    if lowercase(trim(x.CDS.FieldByName('canceltrans').AsString)) = 't' then begin
      new_rec := false;
      history_slist.Add('<c>');
      username := lowercase(x.CDS.FieldByName('username').AsString);
      saved_date    := lowercase(x.CDS.FieldByName('modifieddate').AsString);
      date_string   := saved_date + ','+username+','+trim(x.CDS.FieldByName('cancelremarks').AsString);
      history_slist.Add(date_string);
      x.CDS.Next;
      continue;
    end;
    if trim(x.CDS.FieldByName('fieldname').AsString) <> '' then begin
      username := lowercase(x.CDS.FieldByName('username').AsString);
      saved_date    := lowercase(x.CDS.FieldByName('modifieddate').AsString);
      date_string   := saved_date + ','+username;
      field_name    :=lowercase(x.CDS.FieldByName('fieldname').AsString);
      row_num       :=lowercase(x.CDS.FieldByName('rowno').AsString);
      delete_flg    :=lowercase(x.CDS.FieldByName('delflag').AsString);
      field_string  := field_name + ','+row_num + ','+delete_flg;
      old_value     := lowercase(x.CDS.FieldByName('oldvalue').AsString);
      new_value     := lowercase(x.CDS.FieldByName('newvalue').AsString);
      if saved_date <> prev_saved_date then begin
        history_slist.Add('<d>');
        history_slist.Add(date_string);
      end;
      history_slist.Add('<f>');
      history_slist.Add(field_string);
      history_slist.Add('<o>');
      history_slist.Add(old_value);
      history_slist.Add('<n>');
      history_slist.Add(new_value);
      prev_saved_date := saved_date;
    end;
    x.CDS.Next;
  end;
  history_slist.Add('<end>');
  x.close;
  FreeAndNil(x);
end;

function TASBTStructObj.CreateHistoryData(tstrlst : TstringList ; xd :IXMLDocument) :  IXMLDocument;
var  checkstr,tag_name , field_caption, field_string , transid,recordid,x : string;
     i,change_count : integer;
     isFirstLine : boolean;
     w , SchemaName,p : string;
     nd, vl, fld , n , n1: IXMLNode;
     stm : TStringStream;
begin
    Result := LoadXMLDataFromWS('<root></root>');
    checkstr := ',<d>,<f>,<o>,<n>,<a>,<nt>,<c>,';
    tag_name := '';
    change_count := 0;
    isFirstLine := true;
    for i := 0 to tstrlst.Count-2 do
    begin
      if pos(','+tstrlst[i]+',',checkstr) > 0 then
      begin
        tag_name := tstrlst[i];
        isFirstLine := true;
        continue;
      end;
      if tag_name = '<d>' then
      begin
        inc(change_count);
        nd :=  Result.DocumentElement.AddChild('change'+inttostr(change_count));
        nd.Attributes['modifieddate'] := copy(tstrlst[i],1,pos(',',tstrlst[i])-1);
        nd.Attributes['username'] :=  copy(tstrlst[i],pos(',',tstrlst[i])+1,length(tstrlst[i]));
        if (tstrlst[i+1] = '<nt>') then
         nd.Attributes['newtrans'] := 'yes'
        else
         nd.Attributes['newtrans'] := 'no'
      end
      else if tag_name = '<c>' then
      begin
        inc(change_count);
        field_string := tstrlst[i];
        nd :=  Result.DocumentElement.AddChild('change'+inttostr(change_count));
        nd.Attributes['modifieddate'] := dbm.gf.GetNthString(field_string,1);
        nd.Attributes['username'] :=  dbm.gf.GetNthString(field_string,2);
        nd.Attributes['newtrans'] := 'no';
        nd.Attributes['canceltrans'] := 'yes';
        field_string := copy(field_string,length(nd.Attributes['modifieddate']+nd.Attributes['username'])+3,length(field_string));
        nd.NodeValue := field_string;
      end
      else if tag_name = '<a>' then
      begin
        field_string := tstrlst[i];
        field_caption :=  dbm.gf.GetNthString(field_string,1);
        fld := nd.AddChild('attach');
        fld.Attributes['status'] := field_caption;
        fld.NodeValue := dbm.gf.GetNthString(field_string,2)
      end
      else if tag_name = '<f>' then
      begin
        field_string := tstrlst[i];
        field_caption :=  dbm.gf.GetNthString(field_string,1);
        n1 := xd.documentelement.ChildNodes.FindNode(field_caption) ;
        if n1 <> nil then
        begin
           field_caption := Trim(vartostr(n1.ChildValues['a2']));
        end;
        fld := nd.AddChild('field');
        fld.Attributes['caption'] := field_caption;
        fld.Attributes['rowno'] := dbm.gf.GetNthString(field_string,2);
        fld.Attributes['delflag'] := dbm.gf.GetNthString(field_string,3);
        fld.Attributes['parent_caption'] := dbm.gf.GetNthString(field_string,4);
      end
      else if tag_name = '<o>' then
      begin
        if isFirstLine then vl := fld.AddChild('oldvalue');
        vl.AddChild('l1').NodeValue := tstrlst[i];
        isFirstLine := false;
      end
      else if tag_name = '<n>' then
      begin
        if isFirstLine then vl := fld.AddChild('newvalue');
        vl.AddChild('l1').NodeValue := tstrlst[i];
        isFirstLine := false;
      end;
    end;
end;

function TASBTStructObj.GetSearchResult(s,sxml:string):string;
var x,sqlfld,v,SearchCond:String;
    q:TXDS;
begin
  result := '';
  servicename:='GetSearchResult';
  SearchCond := '';
  try
    dbm := nil;
    dbcall := nil;
    q := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetSearchResult');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to GetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to GetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to GetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['sqlfield']) = varnull then
      raise Exception.create('field tag not specified in call to GetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['value']) = varnull then
      raise Exception.create('value tag not specified in call to GetSearchResult WebService');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if vartype(xmldoc.DocumentElement.Attributes['cond']) <> varnull then
      SearchCond := lowercase(trim(xmldoc.DocumentElement.Attributes['cond']));
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetSearchResult webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        //---
        dbm.gf.dodebug.msg('Received XMl ' + s);
        x:= xmldoc.DocumentElement.Attributes['transid'];
        ASBDataObj.CreateAndSetDbCall(x,sxml);
        dbCall := ASBDataObj.DbCall;
        ASBDataObj.CreateActionObj(x,'tstructs');
        ASBDataObj.SubmitClientValuesToSD;
        dbcall.Validate.loading:=True;
        x := xmldoc.DocumentElement.Attributes['field'];
        sqlfld := xmldoc.DocumentElement.Attributes['sqlfield'];
        v := vartostr(xmldoc.DocumentElement.Attributes['value']);
        fld:=dbcall.struct.GetField(x);
        q:=fld.QSelect;
        result := MakeSearchResult(fld,'',v,SearchCond,q);
        dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg('Error : ' + e.message);
      if pos('access violation',lowercase(e.message)) > 0 then result := CreateErrNode('',false)
      else result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  end;
  closeproject;
  close_err := '';
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;


function TASBTStructObj.AutoGetSearchResult(s,sxml:string):string;
var x,sqlfld,v,SearchCond:String;
    q:TXDS;
begin
  result := '';
  servicename:='AutoGetSearchResult';
  SearchCond := '';
  FastDataFlag := true;
  try
    dbm := nil;
    dbcall := nil;
    q := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in AutoGetSearchResult');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['sqlfield']) = varnull then
      raise Exception.create('field tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['value']) = varnull then
      raise Exception.create('value tag not specified in call to AutoGetSearchResult WebService');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if vartype(xmldoc.DocumentElement.Attributes['cond']) <> varnull then
      SearchCond := lowercase(trim(xmldoc.DocumentElement.Attributes['cond']));
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing AutoGetSearchResult webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        dbm.gf.DisplayTotRows := false;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        //---
        dbm.gf.dodebug.msg('Received XMl ' + s);
        x:= xmldoc.DocumentElement.Attributes['transid'];
        ASBDataObj.CreateAndSetDbCall(x,sxml);
        dbCall := ASBDataObj.DbCall;
        ASBDataObj.CreateActionObj(x,'tstructs');
        ASBDataObj.SubmitClientValuesToSD;
        dbcall.Validate.loading:=True;
        x := xmldoc.DocumentElement.Attributes['field'];
        sqlfld := xmldoc.DocumentElement.Attributes['sqlfield'];
        v := vartostr(xmldoc.DocumentElement.Attributes['value']);
        fld:=dbcall.struct.GetField(x);
        q:=fld.QSelect;
        result := MakeSearchResult(fld,'',v,SearchCond,q);
        dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg('Error : ' + e.message);
      if pos('access violation',lowercase(e.message)) > 0 then result := CreateErrNode('',false)
      else result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  end;
  closeproject;
  close_err := '';
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.GetSearchVal(s,sxml:string): WideString;
var x , sqltext,transcondition,searchorder :String;
    q:TXDS;
    rnode : IXMLNode;
    srch : TSearchVal  ;
    tfld : pfld;
begin
  result := 'done';
  servicename:='Get Search Value';
  try
    q := nil;
    srch := nil;
    searchorder := '';
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
    raise Exception.create('Sessionid not specified in GetSearchVal') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to GetSearchVal WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to GetSearchVal WebService');
    x := xmldoc.documentelement.attributes['axpapp'];
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetSearchVal webservice');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
      result := x
    else
    begin
        x := xmldoc.documentelement.attributes['transid'];
        ASBDataObj.CreateAndSetDbCall(x,sxml);
        dbCall := ASBDataObj.DbCall;
        if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
        ASBDataObj.CreateActionObj(x,'tstructs');
        srch := TSearchVal.Create;
        srch.Axp := axprovider;
        srch.sdef := dbcall.struct;
        srch.sflds := vartostr(xmldoc.DocumentElement.ChildValues['fields']);
        srch.srchfld := vartostr(xmldoc.DocumentElement.ChildValues['searchfor']);
        srch.sval := vartostr(xmldoc.DocumentElement.ChildValues['value']);
        tfld := dbcall.struct.GetField('viewfilter');
        if tfld <> nil then
          dbcall.Validate.RefreshField(tfld,1);
        srch.Parser := dbcall.Parser;
        srch.TransCondition := transcondition;
        srch.searchorder  := vartostr(xmldoc.DocumentElement.Attributes['searchorder']);
        dbm.gf.dodebug.msg('Fields ' + srch.sflds);
        dbm.gf.dodebug.msg('Search Field ' + srch.srchfld);
        dbm.gf.dodebug.msg('Search Value ' + srch.sval);
        dbm.gf.dodebug.msg('Search transcondition ' + TransCondition);
        sqlText := srch.GetSQL ;
        dbm.gf.dodebug.msg('SQL  :' + sqlText);
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        q := TXDS.create('q',nil,connection,dbm.gf);
        q.buffered := True;
        if dbm.gf.pagination_pageno = 1 then
        begin
           s := dbm.ChangeSqlForPagination(sqlText,true);
           q.CDS.CommandText := s;
           assignparams(q);
           q.open;
           dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
           q.close;
        end;
        if (dbm.gf.pagination_totalrows = 0) and (dbm.gf.pagination_pageno = 1)then begin
          rnode := xmldoc.documentelement.AddChild('response');
          rnode.Attributes['totalrows'] := '0';
        end
        else begin
          sqlText := dbm.ChangeSqlForPagination(sqlText,false);
          dbm.gf.dodebug.msg('Sql after pagination change  :' + sqlText);
          q.CDS.CommandText := sqlText;
          assignparams(q);
          q.open;
          rnode := xmldoc.documentelement.AddChild('response');
          if dbm.gf.pagination_totalrows > 0 then rnode.Attributes['totalrows'] := inttostr(dbm.gf.pagination_totalrows);
          writeresultWithCap(q,rnode);
          q.close;
        end;
        result := xmldoc.DocumentElement.XML;
    end;
    dbm.gf.dodebug.msg('Step 0');
    dbm.gf.dodebug.msg('Result : ' + result);
    dbm.gf.dodebug.msg('Step 1');
  except
    on E:Exception do
    begin
     GenerateSessionAppKey := False;
     dbm.gf.dodebug.msg('Step 2');
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,true);
      dbm.gf.dodebug.msg(result);
      dbm.gf.dodebug.msg('Step 3');
    end;
  end;
  dbm.gf.dodebug.msg('Freeing search obj');
  if assigned(srch) then FreeAndNil(srch) ;
  dbm.gf.dodebug.msg('Freeing xds obj');
  if assigned(q) then
  begin
    FreeAndNil(q);
  end;
  dbm.gf.dodebug.msg('Closing Project');
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(xmldoc,'');
end;

function TASBTStructObj.GetRecordId(xml:string):string;
var x,s,nv,ws:String;
    i:integer;
    pval : IXMLNode;
    fld : pfld;
begin
  result := '';
  servicename:='Get transaction record id';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetRecordId') ;
     if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetRecordid');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice GetRecordid');
    x := xmldoc.DocumentElement.Attributes['transid'];
    pval := xmldoc.DocumentElement.ChildNodes.FindNode('values');
    if pval = nil then
      raise Exception.create('Value node not specified in call to webservice GetRecordid');
    if pval.ChildNodes.Count = 0 then
      raise Exception.create('Value node not specified in call to webservice GetRecordid');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetRecordid webservice');
    dbm.gf.dodebug.msg('Reading parameter');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then result := x
    else
    begin
      x:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      ASBDataObj.CreateAndSetDbCall(x,'');
      dbCall := ASBDataObj.DbCall;
      tcstr := ASBDataObj.CheckForAccess(x);
      for i:=0 to pval.childnodes.count-1 do begin
        nv := vartostr(pval.ChildNodes[i].NodeName);
        fld := dbcall.struct.getfield(nv);
        if assigned(fld) then begin
          s := vartostr(pval.ChildNodes[i].NodeValue);
          if fld.DataType = 'd' then
            s := dbm.gf.findandreplace(dbm.gf.dbdatestring, ':value', dbm.gf.ConvertToDBDateTime(dbm.Connection.dbtype,strtodatetime(s)))
          else if (fld.DataType = 'c') or (fld.DataType ='t') then
            s := quotedstr(s);
        end else
          s := quotedstr(vartostr(pval.ChildNodes[i].NodeValue));
        ws:=ws + ' and '+nv+'='+s;
      end;
      if ws<>'' then delete(ws, 1, 5);
      if dbcall.struct.SchemaName <> '' then
         ws:='select ' + lowercase(dbcall.struct.PrimaryTable)+'id' + ' from '+ dbcall.struct.SchemaName + '.' + dbcall.struct.PrimaryTable+' where '+ws
      else
         ws:='select ' + lowercase(dbcall.struct.PrimaryTable)+'id' + ' from '+ dbcall.struct.PrimaryTable+' where '+ws;
      xmldoc:= axprovider.GetOneRecord(ws, '', '');
      dbm.gf.dodebug.msg('Get recordid called : ' + xmldoc.DocumentElement.XML);
      if xmldoc.DocumentElement.childnodes.count>0 then
      begin
        pval := xmldoc.DocumentElement.ChildNodes[0];
        pval := pval.ChildNodes.findnode(lowercase(dbcall.struct.PrimaryTable+'id'));
        if pval <> nil then
           result := '<root><recordid>'+vartostr(pval.NodeValue)+'</recordid></root>'
        else begin
          pval := xmldoc.DocumentElement.ChildNodes[0];
          pval := pval.ChildNodes.findnode(uppercase(dbcall.struct.PrimaryTable+'id'));
          if pval <> nil then
             result := '<root><recordid>'+vartostr(pval.NodeValue)+'</recordid></root>'
        end;
      end
      else result := '<root><recordid>0</recordid></root>';
      xmldoc := LoadXMLData(result);
    end
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.Msg('Error : ' + e.Message);
      result := CreateErrNode(E.Message,true);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(xmldoc,'');
end;

function TASBTStructObj.DeleteRows(S: String): String;
var
  x,sname,dtype,rid,tcstr,tname : String;
  i : Integer;
  n ,rnode : ixmlnode;
begin
  result := '';
  servicename:='Delete rows';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DeleteRows');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to DeleteRows');
    if vartype(xmldoc.DocumentElement.Attributes['sname']) = varnull then
      raise Exception.create('Structure Name tag not specified in call to DeleteRows');
    if vartype(xmldoc.DocumentElement.ChildNodes.FindNode('varlist')) = varnull then
      raise Exception.create('Varlist node not specified in call to DeleteRows');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing DeleteRows webservice');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    dbm.gf.dodebug.msg('Reading parameters');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else begin
      sname := vartostr(xmldoc.documentelement.attributes['sname']);
      rnode := xmldoc.DocumentElement;
      rnode := rnode.ChildNodes.FindNode('varlist');
      if rnode.ChildNodes.Count > 0 then
      begin
          ASBDataObj.CreateAndSetDbCall(sname,'');
          dbcall := ASBDataObj.DbCall;
          dbcall.Dbm := dbm;
          dbcall.axprovider := axprovider;
          dbcall.transid := sname;
          ASBDataObj.CreateActionObj(sname,'tstruct');
          act := ASBDataObj.act;
          tname := lowercase(dbcall.StoreData.PrimaryTableName);
          tcstr := ASBDataObj.CheckForAccess(sname);
          for i:= 0 to rnode.ChildNodes.Count - 1 do
          begin
            dtype := 'c';
            rid := '';
            xnode := rnode.ChildNodes[i];
            n := xnode.ChildNodes.FindNode('recordid');
            if n = nil then
            begin
               n := xnode.ChildNodes.FindNode(tname + 'id');
            end;
            if n <> nil then
            begin
              rid := vartostr(n.NodeValue);
            end;
            if rid <> '' then
            begin
              dbm.gf.dodebug.msg(' Recordid='+rid);
                  dbcall.LoadData(strtofloat(rid)) ;
               if (tcstr = 'd') or (tcstr = '') then
               begin
                   dbm.StartTransaction(connection.ConnectionName);
                   dbcall.DeleteData(strtofloat(rid));
                   if dbcall.ErrorStr <> '' then
                      raise exception.Create(dbcall.ErrorStr)
                   else
                   begin
                      if assigned(axprovider) and (dbm.gf.GenMapTrans) then axprovider.DeleteFromTransCheck;
                      dbm.Commit(connection.ConnectionName);
                      act.createmsgnode('done');
//                      dbm.gf.dodebug.msg('Result : ' + Result);
                   end;
               end else begin
                 result := 'Access denied for deleting this transaction';
                 act.createmsgnode(result);
//                 ASBDataObj.CreateMessageNode;
//                 Result := ASBDataObj.jsonstr;
//                 dbm.gf.dodebug.msg('Result : ' + Result);
               end
            end
            else begin
                 result := 'RecordID Not found to delete the record';
                 act.createmsgnode(result);
//                 ASBDataObj.CreateMessageNode;
//                 Result := ASBDataObj.jsonstr;
            end;
          end;
          ASBDataObj.CreateMessageNode;
          Result := ASBDataObj.jsonstr;
          dbm.gf.dodebug.msg('Result : ' + Result);
      end ;
  end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
      begin
        if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
        dbm.gf.execActName := 'DeleteRows';
        dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        dbm.gf.execActName := '';
      end;
    end;
  end;
  closeproject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.MakeSearchResult(fld : pFld ; sqlfld,v,SearchCond : String ; q : TXDS) : String;
  var s,tmpsql,sqCol,jsonstr : String;
  k : integer;
begin
  result := '';
  jsonstr := '';
  if assigned(q) then
  begin
      GetPickListResult(fld,sqlfld,v,SearchCond);
      k := 0;
      if fld.SourceKey then k := 1;
      if sqlfld = '' then sqlfld := fld.FieldName;
      s := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"},' ;
      jsonstr := WriteResultForGetSearchResult(fld,q,k);
  end;
  if jsonstr <> '' then   jsonstr :=  s + jsonstr + ']}'
  else jsonstr := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"}]}' ;
  dbm.gf.dodebug.msg('JSon String : ' + jsonstr);
  result := jsonstr;
end;

function TASBTStructObj.MakeSearchResultForGetDep(fld : pFld ; sqlfld,v,idupdate : String ; q : TXDS) : String;
  var s,tmpsql,sqCol,jsonstr : String;
  k : integer;
begin
  result := '';
  jsonstr := '';
  if assigned(q) then
  begin
      GetPickListResult(fld,sqlfld,v);
      k := 0;
      if fld.SourceKey then k := 1;
      if sqlfld = '' then sqlfld := fld.FieldName;
      s := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"idupdate":"'+idupdate+'"},{"fname":"'+sqlfld+'"},' ;
      jsonstr := WriteResultForGetSearchResult(fld,q,k);
  end;
  if jsonstr <> '' then   jsonstr :=  s + jsonstr + ']}'
  else jsonstr := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"}]}' ;
  dbm.gf.dodebug.msg('JSon String : ' + jsonstr);
  result := jsonstr;
end;

function TASBTStructObj.MakeResultForGetDep(fld : pFld ; sqlfld,v,idupdate : String ; q : TXDS) : String;
  var s,tmpsql,sqCol,jsonstr : String;
  k : integer;
begin
  result := '';
  jsonstr := '';
  if assigned(q) then
  begin
      if not q.CDS.Active then GetPickListResult(fld,sqlfld,v);
      k := 0;
      if fld.SourceKey then k := 1;
      if sqlfld = '' then sqlfld := fld.FieldName;
      s := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"idupdate":"'+idupdate+'"},{"fname":"'+sqlfld+'"},' ;
      jsonstr := WriteResultForGetSearchResult(fld,q,k);
  end;
  if jsonstr <> '' then   jsonstr :=  s + jsonstr + ']}'
  else jsonstr := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"}]}' ;
  dbm.gf.dodebug.msg('JSon String : ' + jsonstr);
  result := jsonstr;
end;

procedure TASBTStructObj.GetAutoCompleteFillData(Fld:pFld; q:TXDS; var DepTrnFldList: TStringList; var DepQryFldList :TStringList);
var
    i : Integer;
    Dfld : pFld;
    findField: TField;
    Function FieldExistsInCDS(FieldName:String) : Boolean;
    begin
      Result := false;
      findField := nil;
      findField := q.CDS.FindField(FieldName);
      if Assigned(findField) then
        Result := not (findField.Calculated);
    end;
begin
  for i := 0 to fld.Dependents.count-1 do
  begin
    if fld.DependentTypes[i+1]='f' then
    begin
      Dfld := dbcall.struct.GetField(fld.Dependents[i]);
      if Dfld = nil then continue;
      if (Dfld.LinkField = Fld.FieldName) and (Dfld.ModeofEntry='fill') and (FieldExistsInCDS(Dfld.SourceField)) then
      begin
        DepTrnFldList.Add(Dfld.Fieldname);
        DepQryFldList.Add(Dfld.SourceField);
      end;
    end;
  end;
end;

function TASBTStructObj.WriteResultForGetSearchResult(fld : pFld; q:TXDS;scol :integer) : String;
var s,val, DepfldList : String;
  Idx : Integer;
  DepTrnFldList,DepQryFldList : TStringList;
begin
  result := '';
  DepTrnFldList := nil; DepQryFldList := nil;
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
  if q.CDS.RecordCount = 0 then exit;
  q.CDS.First;
  if (FastDataFlag) and ((servicename ='GetSearchResult') or (servicename ='AutoGetSearchResult')) then
  begin
    DepTrnFldList := TStringList.Create;
    DepQryFldList := TStringList.Create;
    if assigned(fld.Dependents) then
      GetAutoCompleteFillData(fld, q, DepTrnFldList, DepQryFldList);
  end;
  s := '';
  while not q.CDS.Eof do
  begin
    val := '';
    val := q.CDS.Fields[sCol].AsString;
    if pos(#$A,val) > 0  then val := dbm.gf.FindAndReplace(val,#$A,'<br>');
    if pos(#$D,val) > 0  then val := dbm.gf.FindAndReplace(val,#$D,'<br>');
    val:=dbm.gf.FindAndReplace(val, '"', '^^dq');
    if val <> '' then
    begin
      if FastDataFlag then
      begin
        DepfldList := '';
        if ((servicename ='GetSearchResult') or (servicename ='AutoGetSearchResult')) and (assigned(DepQryFldList)) and (assigned(DepTrnFldList)) then
        begin
          for Idx := 0 to DepQryFldList.count -1 do
          begin
            if q.CDS.FieldByName(DepQryFldList[Idx]).AsString = '' then
              DepfldList := DepfldList + DepTrnFldList[Idx] + '^'
            else
              DepfldList := DepfldList + DepTrnFldList[Idx] + '~' +q.CDS.FieldByName(DepQryFldList[Idx]).AsString +'^';
          end;
          Delete(DepfldList,length(DepfldList),1);
          DepfldList := dbm.gf.FindAndReplace(DepfldList, '\', '\\');
          DepfldList := dbm.gf.FindAndReplace(DepfldList, '"', '\"');
          If trim(DepfldList) <> '' then
            DepfldList := '","d":"'+DepfldList
        end;
        if fld.SourceKey then val := '{"i":"'+val +'","v":"'+ q.CDS.Fields[0].AsString + DepfldList+ '"}'
        else val := '{"i":"'+val +'","v":"'+DepfldList+'"}';
        s := s + val + ',';
      end
      else
      begin
        if fld.SourceKey then val := q.CDS.Fields[0].AsString + '^' + val;
        s := s + val + '~';
      end;
    end;
    q.CDS.next;
  end;
  delete(s,length(s),1);
  if s <> '' then
  begin
    if FastDataFlag then
      result := '{"dfname":"'+DepfldList+'"},{"data":['+ s + ']}'
    else
      result := '{"fvalue":"' +  s + '"}';
  end;
  dbm.gf.dodebug.msg('JSon String : ' + result);
  if assigned(DepTrnFldList) then
  begin
    DepTrnFldList.Clear;
    FreeAndNil(DepTrnFldList);
  end;
  if assigned(DepQryFldList) then
  begin
    DepQryFldList.Clear;
    FreeAndNil(DepQryFldList);
  end;
end;

function TASBTStructObj.WorkFlowAction(xml:string):string;
  var x,ptable,actname,transid,recid,cmts,s,wfmailauth : String;
      lno,elno : Integer;
      dlgusr : Boolean;
      prnt : TPrintDocs;
      formvalidate : AnsiString;
begin
  prnt := nil;
  result := '';
  servicename:='Workflow Action';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in CallWorkFlowAction');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if vartype(xmldoc.DocumentElement.Attributes['actname']) = varnull then
      raise exception.create('WorkFlow Action attribute not specified');
    if vartype(xmldoc.DocumentElement.Attributes['lno']) = varnull then
      raise exception.create('LevelNo. attribute not specified');
    if vartype(xmldoc.DocumentElement.Attributes['elno']) = varnull then
      raise exception.create('EnblLevelNo. attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if xmldoc.DocumentElement.HasAttribute('usrname') and xmldoc.DocumentElement.HasAttribute('wfmailauth') then
       wfmailauth := lowercase(vartostr(xmldoc.DocumentElement.Attributes['wfmailauth']));
    if (wfmailauth = 'f') then ValidateSessionAppKey := false;
    x := xmldoc.documentelement.attributes['axpapp'];
    openconnect:=True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing CallWorkFlowAction webservice');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    dbm.gf.dodebug.msg('');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else begin
      if (wfmailauth = 'f') then
      begin
        dbm.gf.evalCopy:= true;
        dbm.gf.username := vartostr(xmldoc.DocumentElement.Attributes['usrname']);
        dbm.gf.dodebug.msg('Laoding global vars for workflow without authentication');
//        dbm.gf.sessionid := 'WF'+'abcdefghijklmnopqrs'+inttostr(Random(100));
        ASBCommonObj.GetUserRoleResponsibility(dbm.gf.username);
        dbm.gf.dodebug.msg('User Group '+dbm.gf.usergroup);
        dbm.gf.dodebug.msg('User Roles '+dbm.gf.userroles);
        ASBCommonObj.LoadAppVars;
      end;
      dbm.gf.dodebug.msg('Creating DbCall');
      dbcall := TDbCall.Create;
      transid:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recid:= vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      actname:= vartostr(xmldoc.DocumentElement.Attributes['actname']);
      cmts:= vartostr(xmldoc.DocumentElement.Attributes['comments']);
      lno := StrToInt(vartostr(xmldoc.DocumentElement.Attributes['lno']));
      elno := StrToInt(vartostr(xmldoc.DocumentElement.Attributes['elno']));
      dlgusr := vartostr(xmldoc.DocumentElement.Attributes['dlgusr'])='true';
      formvalidate := '';
      if vartype(xmldoc.DocumentElement.Attributes['formval']) <> varnull then
         formvalidate := vartostr(xmldoc.DocumentElement.Attributes['formval']);
      ASBDataObj.CreateAndSetDbCall(transid,'');
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.DbCall.act := ASBDataObj.act;

      dbcall.Parser.Registervar('recordid', 'n', recid);
      ptable:=dbcall.struct.PrimaryTable;
      dbcall.CreateMapObjects;
      dbcall.Parser.OnLoadAndSave:=ASBDataObj.LoadAndSave;
      dbcall.Parser.OnLoadTrans := LoadTrans;
      dbcall.Parser.OnSaveTrans:=SaveTrans;
      dbcall.Parser.OnEndTrans := EndTrans;
      dbcall.SaveTrans := SaveTrans;
      dbcall.Parser.OnCopyTransAndSave := CopyTransAndSave;
      dbcall.Parser.OnSendMail := SendMail;
      if vartostr(xmldoc.DocumentElement.Attributes['changed']) = 'true' then
      begin
          dbcall.LoadData(strtofloat(recid));
          s := SubmitAndValidateInWFAction(recid);
          if s <> '' then raise exception.Create(s);
          dbm.StartTransaction(connection.ConnectionName);
          dbcall.Parser.OnSaveTrans := dbcall.WorkFlow.SaveTrans;
          dbcall.WorkFlow.SetAction(actname,ptable,transid,recid,cmts,lno,elno,dlgusr);
          if dbcall.WorkFlow.ErrString <> '' then
            Raise Exception.Create(dbcall.WorkFlow.ErrString);
          dbm.gf.dodebug.Msg('Workflow status : ' + dbcall.WorkFlow.AppStatus);
          dbcall.Parser.OnSaveTrans := SaveTrans;
          s :=SaveDataInWFAction;
          if s <> '' then raise exception.Create(s);
          dbm.Commit(connection.connectionname);
      end else begin
        if formvalidate = 'true' then
        begin
          dbcall.StoreData.LoadTrans(strtofloat(recid));
          dbcall.Validate.Validation:=true;
          dbcall.Validate.Loading:=false;
          dbcall.Validate.FillAndValidate(false);
          if dbcall.Validate.ErrorStr <> '' then
            Raise Exception.Create(dbcall.Validate.ErrorStr);
        end else
        begin
          dbcall.StoreData.LoadTrans(strtofloat(recid));
          dbcall.Validate.Validation:=false;
          dbcall.Validate.Loading:=true;
          dbcall.Validate.FillAndValidate(false);
        end;
        dbm.StartTransaction(connection.connectionname);
        dbcall.Parser.OnSaveTrans := dbcall.WorkFlow.SaveTrans;
        dbcall.WorkFlow.SetAction(actname,ptable,transid,recid,cmts,lno,elno);
        if dbcall.WorkFlow.ErrString <> '' then
          Raise Exception.Create(dbcall.WorkFlow.ErrString);
        //for handling genmap & mdmap if onapprove or onreject set to true in def of genmap or mdmap.
        dbcall.Parser.OnSaveTrans := SaveTrans;
        dbcall.Validate.AutoAddToList := true;
        dbcall.Validate.FromMap := true;
        dbcall.Validate.Validation:=True;
        dbcall.Validate.Loading:=False;
//        if dbcall.WorkFlow.chkmailcont then
//        begin
//          prnt := TPrintDocs.create;
//          prnt.Axp := AxProvider;
//          prnt.storedata := dbcall.storedata;
//          prnt.parser := dbcall.parser;
//          prnt.transid := transid;
//          dbcall.workflow.OnPrintDocForm := prnt.PrintDocForm;
//          dbcall.workflow.OnPrintPDFaws := prnt.PDFPrint;
//          dbcall.workflow.applname := vartostr(xmldoc.DocumentElement.Attributes['axpapp']);
//          dbcall.WorkFlow.SendMailWorkflow;
//        end;
      end;
      if dbcall.Workflow.ToBeSaved then
      begin
        dbcall.CallFromSaveDataInWFAction := True;
        SaveTrans;
        dbcall.CallFromSaveDataInWFAction := False;
      end else begin
        dbcall.ClearFields := False;
        dbcall.WorkFlowProcessed := True;
        DbCall.MapData;
        dbcall.ClearFields := True;
        dbcall.WorkFlowProcessed := False;
      end;
      if dbCall.ErrorStr <> ''  then
      begin
         raise exception.Create(dbCall.ErrorStr);
      end;
      if dbm.Intransaction then dbm.Commit(connection.ConnectionName);
      dbm.gf.dodebug.msg('Transaction commited');
      //Process RMQ Message
      ProcessRMQMessages;
      if dbcall.WorkFlow.chkmailcont then
      begin
        prnt := TPrintDocs.create;
        prnt.Axp := AxProvider;
        prnt.storedata := dbcall.storedata;
        prnt.parser := dbcall.parser;
        prnt.transid := transid;
        dbcall.workflow.OnPrintDocForm := prnt.PrintDocForm;
        dbcall.workflow.OnPrintPDFaws := prnt.PDFPrint;
        dbcall.workflow.applname := vartostr(xmldoc.DocumentElement.Attributes['axpapp']);
        dbcall.WorkFlow.SendMailWorkflow;
      end;
    end;
    result := 'done';
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg(E.Message);
      result := CreateErrNode(E.Message,true);
      if assigned(dbm.Connection) then
      begin
        if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
        x := dbm.GetAxpertMsg(e.Message);
        if x <> '' then
        begin
           result := CreateErrNode(x,true);
           dbm.gf.dodebug.msg('Error : ' + x);
        end;
      end;
    end;
  end;
  if prnt <> nil then
  begin
     prnt.Free;
     prnt := nil;
  end;
  GenerateSessionAppKey := False;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,true);
end;

function TASBTStructObj.ViewComments(xml:string):string;
  var x,transid,recid : String;
begin
  result := '';
  servicename:='View Comments';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in LoadNotification') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing LoadNotification webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else begin
      dbm.gf.dodebug.msg('Loading Work Flow Notification');
      transid:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recid:= vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      if not assigned(DbCall) then begin
        ASBDataObj.CreateAndSetDbCall(transid,'');
        dbCall := ASBDataObj.DbCall;
        ASBDataObj.CreateActionObj(x,'tstructs');
        ASBDataObj.DbCall.act := ASBDataObj.act;
        dbcall.Workflow.Schema := dbcall.StoreData.CompanyName;
      end;
      xmldoc := dbcall.WorkFlow.SendHistory(transid,recid);
      result := xmldoc.DocumentElement.XML;
      dbm.gf.dodebug.Msg('Result : ' + result);
    end
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg(E.Message);
      result := CreateErrNode(E.Message,true);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,true)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(xmldoc,'');
end;

procedure TASBTStructObj.DeletePopFldsFromDepFieldList(flist : TStringlist);
  var j,pidx : integer;
begin
  for j := flist.Count - 1 downto 0 do
  begin
     fld:= dbcall.struct.GetField(flist.Strings[j]);
     if not assigned(fld) then continue;
     if (pfrm(dbcall.struct.frames[fld.FrameNo-1]).Popup) then
     begin
       pidx:=pfrm(dbcall.struct.frames[fld.FrameNo-1]).popindex;
       if pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo = fld.FrameNo then
       begin
          flist.Delete(j);
       end;
     end;
  end;
end;

function TASBTStructObj.ValidatePickListValue(fldName:String;fno,actrow:integer;flist:TStringList) : String;
  var s,v,dcn,dcno,dcnos,tmpsql :String;
      i,frmno : integer;
      asgrid : boolean;
begin
  result := '';
  s := '';
  if (pfrm(dbcall.struct.frames[fld.FrameNo-1]).Popup) then
     dbcall.Validate.GetParentActiveRow(actrow,pfrm(dbcall.struct.frames[fld.FrameNo-1]).popindex);
  if assigned (fld.QSelect) then tmpsql := fld.QSelect.CDS.CommandText;
  dbcall.Validate.RegRow(fld.FrameNo,actrow);
  dbcall.Validate.EnterField(fld.FieldName,actrow);
  if dbcall.Validate.ErrorStr = '' then
  begin
    v := dbcall.StoreData.GetFieldValue(fld.FieldName,actrow);
    dbm.gf.dodebug.msg('PList Field Value : ' + v);
    dbcall.Validate.ExitField(fld.FieldName,actrow,v,0);
  end;
  s := dbcall.Validate.ErrorStr;
  dbm.gf.dodebug.msg('Validate error message : ' + s);
  asgrid := false;
  if s <> '' then
  begin
    if assigned (fld.QSelect) then fld.QSelect.CDS.CommandText := tmpsql; //to use for getsearchvalue
    s:=dbm.gf.FindAndReplace(s, '"', '^^dq');
    ASBDataObj.act.createmsgnode(s);
    result := s;
    frmno := 0;
    fld := DbCall.struct.GetField(fldName);
    if assigned(fld) then asgrid := fld.AsGrid;
    for i := 0 to flist.Count - 1 do
    begin
      s := flist.Strings[i];
      dbm.gf.dodebug.msg('Filed Name : ' + s);
      fld:=dbcall.struct.GetField(s);
      if not assigned(fld) then continue;
      if not fld.AsGrid then
      begin
        DbCall.StoreData.SubmitValue(fld.FieldName, 1, '', '', 0, 0, 0);
      end else if (fld.AsGrid) and (fno = fld.FrameNo) then
      begin
       DbCall.StoreData.SubmitValue(fld.FieldName, actrow, '', '', 0, 0, 0);
      end;
//      dbcall.SubmitValue(fld.FieldName, '', '', 0, 0, 0);
      dcn := quotedstr('dc' + inttostr(fld.FrameNo));
      // newly added to fill data using fillgrid def
      if (not asgrid) then
      begin
        if fld.FrameNo <> frmno then
        begin
           if pos(dcn,VisibleDCs) > 0 then
           begin
             dcno := quotedstr(inttostr(fld.FrameNo));
             if pos(dcno,dcnos) = 0 then
             begin
                dbm.gf.DoDebug.msg('Calling FindandClearFG');
                FindandClearFG('dc' + inttostr(fld.FrameNo)) ;
                dcnos := dcnos + dcno + ',';
             end;
           end;
           frmno := fld.FrameNo;
        end;
      end;
    end;
  end;
  dbm.gf.dodebug.msg('Validate picklist Result : ' + result);
end;

function TASBTStructObj.FastFieldChoices(xml, sxml: string): widestring;
begin
  FastDataFlag := True;
  Result := GetFieldChoices(xml, sxml);
end;

function TASBTStructObj.FastDoFillGridValues(xml, sxml: string): string;
begin
  FastDataFlag := True;
  Result := DoFillGridValues(xml, sxml);
end;

function TASBTStructObj.FastDoFormLoad(xml, sxml: string): string;
begin
  FastDataFlag := True;
  Result := DoFormLoad(xml, sxml);
end;

function TASBTStructObj.FastGetDepentendFieldValues(xml, sxml: string): string;
begin
  FastDataFlag := True;
  Result := GetDepentendFieldValues(xml, sxml);
end;

function TASBTStructObj.FastLoadData(xml, sxml: String): string;
begin
  FastDataFlag := True;
  Result := LoadData(xml, sxml);
end;

function TASBTStructObj.FastLoadDCCombos(xml, sxml: string): string;
begin
  FastDataFlag := True;
  Result := LoadDCCombos(xml, sxml);
end;

function TASBTStructObj.FastGetSearchResult(xml, sxml: string): string;
begin
  FastDataFlag := true;
  Result := GetSearchResult(xml, sxml);
end;

procedure TASBTStructObj.FillPopUpForDisplayField(fldName : String ; fno,rowno : integer);
  var pidx,m,rc : integer;
      pdc,popdc,newpval : String;
begin
  // new logic to fill popup grid dependency ifany in display field
  pdc := ''; popdc := '';
  if dbcall.struct.popgrids.Count > 0 then
  begin
    for pidx := 0 to dbcall.struct.popgrids.Count - 1 do
    begin
       if pPopGrid(dbcall.struct.popgrids[pidx]).Parent = 'dc'+inttostr(fno) then
       begin
         if (fld.FieldName = pPopGrid(dbcall.struct.popgrids[pidx]).DispField) or
            (fldName = pPopGrid(dbcall.struct.popgrids[pidx]).DispField) then
         begin
           pdc := quotedstr('dc'+inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
           if pos(pdc,popdc) = 0 then
           begin
              dbm.gf.dodebug.msg('Calling FillPopup frame no : ' + inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
              m := 1;
              rc := 1;
              while m <= rc do begin
                if dbcall.Validate.GetRowValidity(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m) = 0 then begin
                  dbcall.StoreData.DeleteRow(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m);
                  rc := dbcall.StoreData.GetRowCount(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                end else inc(m);
              end;
              if (assigned(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill)) and (Trim(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill.Text) = '') then
              begin
                if pPopGrid(dbcall.struct.popgrids[pidx]).FirmBind then
                begin
                  if dbcall.Validate.FillPopupall(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                  begin
                    ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                    popdc := popdc + pdc + ',';
                    dbcall.Validate.ActualRows.Clear;
                  end;
                end else
                begin
                  if dbcall.Validate.IsParentFieldsBound(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                  begin
                    //dbcall.GetParentValue(pidx,RowNo,dbcall.ParentList);
                    GetParentValue(fldName,pidx,RowNo);
                    dbcall.ActualRows.CommaText := dbcall.GetActualRows(pidx,dbcall.ParentList);
                    if dbcall.ActualRows.Count = 0 then begin
                      PopupAutoFill('','','',pidx,rowno);
                      dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                      ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                      popdc := popdc + pdc + ',';
                      dbcall.Validate.ActualRows.Clear;
                    end
                    else begin
                      newpval := dbcall.StoreData.GetFieldValue(fldName,rowno);
                      if oldpval <> newpval then
                      begin
                          PopupAutoFill(FldName,oldpval,newpval,pidx,rowno);
                          dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                          ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                          popdc := popdc + pdc + ',';
                          dbcall.Validate.ActualRows.Clear;
                      end;
                    end;
                  end;
                end;
                break;
              end;
           end;
         end;
       end;
    end;
  end;
end;

function TASBTStructObj.FillPopUpForShowBtnField(fldName : String ; fno,rowno : integer) : boolean;
  var pidx,m,rc,l : integer;
      sb,sb1,sb2,pdc,popdc,newpval : String;
begin
// new logic to fill popup grid dependency ifany
  result := false;
  if dbcall.struct.popgrids.Count > 0 then
  begin
    for pidx := 0 to dbcall.struct.popgrids.Count - 1 do
    begin
       if pPopGrid(dbcall.struct.popgrids[pidx]).Parent = 'dc'+inttostr(fno) then
       begin
         sb := pPopGrid(dbcall.struct.popgrids[pidx]).ShowButtons + ',';
         l := 1;
         while True do
         begin
           sb1 := dbm.gf.GetNthString(sb,l);
           if sb1 = '' then break;
           sb2 := sb1;
           inc(l);
         end;
         if (fld.FieldName = sb2) or
           (fldName = sb2) then
         begin
           pdc := quotedstr('dc'+inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
           if pos(pdc,popdc) = 0 then
           begin
              dbm.gf.dodebug.msg('Calling FillPopup frame no : ' + inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
              rc := dbcall.StoreData.GetRowCount(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
              m := 1;
              while m <= rc do begin
                if dbcall.Validate.GetRowValidity(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m) = 0 then begin
                  dbcall.StoreData.DeleteRow(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m);
                  rc := dbcall.StoreData.GetRowCount(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                end else inc(m);
              end;
              if (assigned(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill)) and (Trim(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill.Text) <> '') then
              begin
                if pPopGrid(dbcall.struct.popgrids[pidx]).FirmBind then
                begin
                  if dbcall.Validate.FillPopupall(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                  begin
                    ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                    popdc := popdc + pdc + ',';
                    dbcall.Validate.ActualRows.Clear;
                    result := true;
                  end;
                end else
                begin
                  if dbcall.Validate.IsParentFieldsBound(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                  begin
//                    dbcall.GetParentValue(pidx,RowNo,dbcall.ParentList);
                    GetParentValue(fldName,pidx,RowNo);
                    dbcall.ActualRows.CommaText := dbcall.GetActualRows(pidx,dbcall.ParentList);
                    if dbcall.ActualRows.Count = 0 then begin
                      PopupAutoFill('','','',pidx,rowno);
                      dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                      ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                      popdc := popdc + pdc + ',';
                      dbcall.Validate.ActualRows.Clear;
                    end else begin
                      newpval := dbcall.StoreData.GetFieldValue(fldName,rowno);
                      if oldpval <> newpval then
                      begin
                          PopupAutoFill(FldName,oldpval,newpval,pidx,rowno);
                          dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                          ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                          popdc := popdc + pdc + ',';
                          dbcall.Validate.ActualRows.Clear;
                      end;
                    end;
                  end;
                  result := true;
                end;
                break;
              end;
           end;
         end;
       end;
    end;
  end;
end;

function TASBTStructObj.CheckAndFillPopUpForParentField(fldName : String ; fno,rowno : integer) : boolean;
  var pidx,m,rc,l : integer;
      sb,sb1,sb2,pdc,popdc,newpval : String;
begin
// new logic to fill popup grid dependency ifany
  result := false;
  if dbcall.struct.popgrids.Count > 0 then
  begin
    for pidx := 0 to dbcall.struct.popgrids.Count - 1 do
    begin
       if pPopGrid(dbcall.struct.popgrids[pidx]).Parent = 'dc'+inttostr(fno) then
       begin
         sb := pPopGrid(dbcall.struct.popgrids[pidx]).ParentField + ',';
         l := 1;
         while True do
         begin
           sb1 := dbm.gf.GetNthString(sb,l);
           if sb1 = '' then break;
           sb2 := sb1;
           inc(l);
           if (fld.FieldName = sb2) or
             (fldName = sb2) then
           begin
             pdc := quotedstr('dc'+inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
             if pos(pdc,popdc) = 0 then
             begin
                dbm.gf.dodebug.msg('Calling FillPopup frame no : ' + inttostr(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo));
                rc := dbcall.StoreData.GetRowCount(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                m := 1;
                while m <= rc do begin
                  if dbcall.Validate.GetRowValidity(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m) = 0 then begin
                    dbcall.StoreData.DeleteRow(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,m);
                    rc := dbcall.StoreData.GetRowCount(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                  end else inc(m);
                end;
                if (assigned(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill)) and (Trim(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill.Text) <> '') then
                begin
                  if pPopGrid(dbcall.struct.popgrids[pidx]).FirmBind then
                  begin
                    if dbcall.Validate.FillPopupall(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                    begin
                      ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                      popdc := popdc + pdc + ',';
                      dbcall.Validate.ActualRows.Clear;
                      result := true;
                    end;
                  end else
                  begin
                    if dbcall.Validate.IsParentFieldsBound(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo,rowno) then
                    begin
  //                    dbcall.GetParentValue(pidx,RowNo,dbcall.ParentList);
                      GetParentValue(fldName,pidx,RowNo);
                      dbcall.ActualRows.CommaText := dbcall.GetActualRows(pidx,dbcall.ParentList);
                      if dbcall.ActualRows.Count = 0 then begin
                        PopupAutoFill('','','',pidx,rowno);
                        dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                        ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                        popdc := popdc + pdc + ',';
                        dbcall.Validate.ActualRows.Clear;
                      end else begin
                        newpval := dbcall.StoreData.GetFieldValue(fldName,rowno);
                        if oldpval <> newpval then
                        begin
                            PopupAutoFill(FldName,oldpval,newpval,pidx,rowno);
                            dbcall.Validate.ActualRows.Assign(dbcall.ActualRows);
                            ProcessPopField(pPopGrid(dbcall.struct.popgrids[pidx]).FrameNo);
                            popdc := popdc + pdc + ',';
                            dbcall.Validate.ActualRows.Clear;
                        end;
                      end;
                    end;
                    result := true;
                  end;
                  break;
                end;
             end;
           end;
           if result = true then break;
         end;
       end;
    end;
  end;
end;

procedure TASBTStructObj.ProcessPopField(fn : integer);
  var i,j,rn : integer;
  v : String;
begin
   if dbcall.Validate.ActualRows.Count > 0 then
   begin
     for j := 0 to dbcall.Validate.ActualRows.Count-1 do
     begin
       rn := StrToInt(dbcall.Validate.ActualRows[j]);
       dbcall.Validate.GetParentActiveRow(rn,pfrm(dbcall.struct.frames[fn-1]).popindex);
       for i:=0 to dbcall.struct.flds.Count-1 do
       begin
           if pfld(dbcall.struct.flds[i]).FrameNo = fn then
           begin
              dbCall.Validate.EnterField(pfld(dbcall.struct.flds[i]).FieldName,rn);
              v := dbCall.StoreData.GetFieldValue(pfld(dbcall.struct.flds[i]).FieldName,rn);
              dbCall.Validate.ExitField(pfld(dbcall.struct.flds[i]).FieldName,rn,v,0);
           end;
       end;
     end;
   end;
end;

function TASBTStructObj.GetQoutedVisibleDCNames(dcname : String) : String;
   var i : integer;
       s : String;
begin
  result := '';
  if dcName <> '' then
  begin
    i := 1;
    while true do
    begin
      s := dbm.gf.GetnthString(dcName,i);
      if s = '' then break;
      result := result + quotedstr(s) + ',' ;
      i := i + 1;
    end;
    delete(result,length(result),1);
  end;
end;

procedure TASBTStructObj.FormLoadJSON;
var i, PriorFrame, PriorRow, RCount, j,k,prcount  : integer;
    fld:pFld;
    frec:pFieldRec;
    stime:TDateTime;
    fg:pFg;
    WithDropDown: Boolean;
    fm : pFrm;
    popgrid : ppopgrid;
    v : string;
begin
  dbm.gf.dodebug.msg('Form Load JSON');
  Dbcall.validate.FormLoadPrepareField := true;
  stime:=now;
  asbDataObj.JSONStr:='';
  PriorRow:=0; PriorFrame:=0;
  fg:=nil;
  i:=0;
  while i<dbcall.struct.flds.Count do begin
    fld := pfld(dbcall.struct.flds[i]);
    WithDropDown := not ((pos(quotedstr('dc' + inttostr(fld.FrameNo)),VisibleDCs) = 0) and (visibleDCs<>''));

    if priorframe <> fld.frameno then begin
      RCount := dbcall.StoreData.GetRowCount(i);
      if RCount = 0  then  RCount:=1;
      PriorFrame := fld.FrameNo;
      PriorRow:=0;
      fg:=ExecFillGrid(fld.FrameNo, fgFromFormLoad,'');
      if assigned(fg) then begin
        i:=AsbDataObj.GridToJSON(fld.FrameNo, WithDropDown);
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields,dbcall.Struct.GetFrame(fld.FrameNo));
        fg:=nil;
        continue;
      end;
      fm:=dbcall.Struct.GetFrame(fld.FrameNo);
      if fm.Popup then
      begin
        popgrid:=nil;
        for j := 0 to dbcall.struct.popgrids.count-1 do begin
          if pPopGrid(dbcall.struct.popgrids[j]).FrameNo=fld.FrameNo then begin
            popgrid:=pPopGrid(dbcall.struct.popgrids[j]);
            break;
          end;
        end;
        if not assigned(popgrid) then
        begin
          i :=fm.StartIndex+fm.FieldCount;
          continue;
        end;
        rcount:=dbcall.storedata.RowCount(popgrid.ParentFrameNo);
        if rcount > 0 then
        begin
          prcount := dbcall.storedata.RowCount(popgrid.FrameNo);
          asbDataObj.PopDCToJSON(fld.FrameNo, prcount);
          for j := 1 to rcount do
          begin
            dbcall.validate.RegRow(popgrid.ParentFrameNo, j);
            i := AsbDataObj.PopGridToJSON(popgrid, j, true);
          end;
        end;
      end;
    end;

    if fld.DataRows.count=0 then
      frec:=Dbcall.validate.PrepareField(fld)
    else begin
      if fld.AsGrid then begin
        i:=AsbDataObj.GridToJSON(fld.FrameNo, WithDropDown);
        continue;
      end else begin
        k:=strtoint(fld.datarows[0]);
        frec:=pFieldRec(dbcall.StoreData.FieldList[k]);
        if fld.txtSelection then
           frec:=Dbcall.validate.PrepareField(fld)
        else if WithDropDown then dbcall.Validate.LoadDropDown(fld, frec);
      end;
    end;
    if fld.DataType = 'i' then begin
      if fld.Tablename <> '' then
        asbDataObj.ImageFieldToJSON(fld)
      else begin
        If (fld.Exprn >= 0) Then Begin
          If Dbcall.Parser.EvalPrepared(fld.Exprn) Then
          begin
            v := Dbcall.Parser.Value;
            if v <> '' then
              asbDataObj.ImageFieldToJSON(fld,v)
          end;
        end;
      end;
    end;
    inc(i);
    if not assigned(frec) then continue;
    asbDataObj.FieldToJSON(fld, frec);
  end;
  asbDataObj.EndDataJSON;
  dbm.gf.dodebug.msg('Form load JSON completed '+inttostr(millisecondsbetween(now, stime)));
end;

procedure TASBTStructObj.LoadDCJSON(FrameNo:Integer;nofill : boolean);
var i , j, k, RCount : integer;
    dcn,v : String;
    frec:pFieldRec;
    fg:pFg;
begin
  dbm.gf.dodebug.msg('Load DC JSON');
//  pFrm(dbcall.struct.frames[frameno-1]).HasDataRows := false;
  fg:=nil;
  asbDataObj.JSONStr:='';
  if  (dbcall.StoreData.LastSavedRecordId=0) and (not nofill) then
    fg:=ExecFillGrid(Frameno, fgFromLoadDC,'');
  RCount := dbcall.StoreData.RowCount(FrameNo);
  if RCount = 0  then  RCount:=1;
  asbDataObj.DCToJSON(FrameNo,RCount);
  if (assigned(fg)) then
  begin
    AsbDataObj.GridToJSON(FrameNo, true) ;
    AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
    PopGridJSON(FrameNo,RCount);
  end else
    AsbDataObj.LoadDCToJSON(FrameNo);
  asbDataObj.EndDataJSON;
  dbm.gf.dodebug.msg('Load DC JSON Completed');
end;

procedure TASBTStructObj.LoadSubGridJSON(FrameNo:Integer);
var i , j, k, RCount : integer;
    dcn,v : string;
    frec:pFieldRec;
    fg:pFg;
begin
  dbm.gf.dodebug.msg('Load Subgrid JSON');
  fg:=nil;
  asbDataObj.JSONStr:='';
  AsbDataObj.LoadSubGridToJSON(FrameNo);
  asbDataObj.EndDataJSON;
  dbm.gf.dodebug.msg('Load DC JSON Completed');
end;

procedure TASBTStructObj.LoadDataJSON;
var i , j, k, RCount, PriorRow, PriorFrame, r  : integer;
    dcn,v : String;
    frec:pFieldRec;
    WithDropDown: Boolean;
    fm:pFrm;

    bIsAmendTrans : Boolean;
    sPEGStatus, sAmendmentStatus, sDataJson : String;
begin
  dbm.gf.dodebug.msg('Load Data JSON');
  asbDataObj.JSONStr:='';
  priorframe:=0;PriorRow:=0;
  withdropdown:=true;
  i:=0;
  While (i<dbcall.struct.flds.Count) do begin
    fld := pfld(dbcall.struct.flds[i]);

    fm:=dbcall.Struct.GetFrame(fld.FrameNo);
    if fm.Popup then
    begin
      inc(i);
      continue;
    end;

    if priorframe <> fld.frameno then begin
      RCount := dbcall.StoreData.RowCount(fld.frameno);
      if RCount = 0  then
      begin
         r:=1;
         AsbDataObj.DummyDCNodeToJSON(fld.FrameNo,r,'i1');  // as per the requirement for .NET
      end else r:=RCount;
      PriorFrame := fld.FrameNo;
      PriorRow:=0;
      WithDropDown := not ((pos(quotedstr('dc' + inttostr(fld.FrameNo)),VisibleDCs) = 0) and (visibleDCs<>''));
      if fld.AsGrid then
      begin
        i:=asbDataObj.GridToJSON(fld.FrameNo, WithDropDown);
        if RCount > 0  then PopGridJSON(fld.FrameNo,rcount);
        continue;
      end;
    end;

    if fld.datarows.count>0 then begin
      for j := 0 to fld.datarows.count-1 do begin
        k:=strtoint(fld.datarows[j]);
        if pFieldRec(dbcall.storedata.fieldlist[k]).RowNo<0 then continue;
        if (fld.ModeOfEntry='select') and (not fld.FromList) and (not fld.txtselection) and (WithDropDown) then
          Dbcall.validate.LoadDropDown(fld, pFieldRec(dbcall.storedata.fieldlist[k]));
        asbDataObj.FieldToJSON(fld, pFieldRec(dbcall.storedata.fieldlist[k]));
      end;
    end else begin
      frec:=nil;
      if (fld.ModeOfEntry='select') and (not fld.FromList) and (not fld.txtselection) and (WithDropDown) then
        frec:=dbcall.validate.LoadDropDown(fld, nil);
      if assigned(frec) then
        asbDataObj.FieldToJSON(fld, frec)
      else if fld.DataType = 'i' then begin
        if fld.Tablename <> '' then
          asbDataObj.ImageFieldToJSON(fld)
        else begin
          If (fld.Exprn >= 0) Then Begin
            If Dbcall.Parser.EvalPrepared(fld.Exprn) Then
            begin
              v := Dbcall.Parser.Value;
              if v <> '' then
                asbDataObj.ImageFieldToJSON(fld,v)
            end;
          end;
        end;
      end;
    end;
    inc(i);
  end;
  asbDataObj.EndDataJSON;

  //Add PEG | Amend along with loaddata response if exists
  if (dbcall.StoreData.structdef.isPegAttached or dbcall.StoreData.structdef.IsAmendmentEnabled)
     and (AnsiStartsStr('{"data":[',asbDataObj.jsonstr)) then
  begin
    sAmendmentStatus := dbm.gf.GetNthString(dbm.gf.sAxPegStatus,1,'$#*#$'); // read Amendment status
    sPEGStatus := dbm.gf.GetNthString(dbm.gf.sAxPegStatus,2,'$#*#$'); //Read PEG status

    bIsAmendTrans := CompareText(dbm.gf.GetNthString(sAmendmentStatus,1,'$##$'),'true') = 0;
    if bIsAmendTrans {dbcall.StoreData.structdef.IsAmendmentEnabled} then //if Amend trans
    begin
      sDataJson := dbm.gf.GetNthString(sAmendmentStatus,4,'$##$');
      if sDataJson = '' then
        sDataJson := '{}';
      asbDataObj.jsonstr := asbDataObj.jsonstr +'*$*'+'{"axamend":[{"readonlytrans":"'+ifthen(dbm.gf.bAxPegReadOnlyTrans,'true','false')+'"'+
                            ',"status":"'+dbm.gf.GetNthString(sAmendmentStatus,2,'$##$')+'"'+  //status
                            ',"enableactions":"'+dbm.gf.GetNthString(sAmendmentStatus,3,'$##$')+'"'+  //enable peg action buttons
                            //data we need as in JSON object format so removed string quotes for values
                            ',"data":'+sDataJson+''+  //amend data
                            ',"withdraw":"'+dbm.gf.GetNthString(sAmendmentStatus,5,'$##$')+'"'+  //withdraw
                            ',"confirmmsg":"'+dbm.gf.GetNthString(sAmendmentStatus,6,'$##$')+'"'+  //Amend confirm msg
                            ',"displaymsg":"'+dbm.gf.GetNthString(sAmendmentStatus,7,'$##$')+'"'+  //AmendStatus for Display purpose
                            ',"comments":"'+dbm.gf.GetNthString(sAmendmentStatus,8,'$##$')+'"'+  //Approver comments
                            ',"recall":"'+dbm.gf.GetNthString(sAmendmentStatus,9,'$##$')+'"'+  //recall
                            '}]}';
    end;
    //if PEG attached
    if dbcall.StoreData.structdef.isPegAttached then
    begin
    sDataJson := dbm.gf.GetNthString(sPEGStatus,4,'$##$');
    if sDataJson = '' then
      sDataJson := '{}';
    asbDataObj.jsonstr := asbDataObj.jsonstr +'*$*'+'{"axpeg":[{"readonlytrans":"'+ifthen(dbm.gf.bAxPegReadOnlyTrans,'true','false')+'"'+
                          ',"status":"'+dbm.gf.GetNthString(sPEGStatus,2,'$##$')+'"'+  //status
                          //if amendment enabled then disable peg action buttons
                          ',"enableactions":"'+dbm.gf.GetNthString(sPEGStatus,3,'$##$')+'"'+  //enable peg action buttons
                          ',"data":'+sDataJson+''+  //peg data
                          ',"withdraw":"'+dbm.gf.GetNthString(sPEGStatus,5,'$##$')+'"'+  //withdraw
                          ',"confirmmsg":"'+dbm.gf.GetNthString(sPEGStatus,6,'$##$')+'"'+  //Peg confirm msg |not reuqired now
                          ',"displaymsg":"'+dbm.gf.GetNthString(sPEGStatus,7,'$##$')+'"'+  //Peg Status for Display purpose |not reuqired now
                          ',"comments":"'+dbm.gf.GetNthString(sPEGStatus,8,'$##$')+'"'+  //Approver comments
                          ',"recall":"'+dbm.gf.GetNthString(sPEGStatus,9,'$##$')+'"'+  //recall
                          ',"finalapproval":"'+dbm.gf.GetNthString(sPEGStatus,10,'$##$')+'"'+  //Final approval done or not
                          '}]}';
    end;
  end;

  dbm.gf.dodebug.msg('Load Data JSON Completed');
end;

procedure TASBTStructObj.FindAndExeFormLoadAction;
    var actnode,anode : ixmlnode;
    i : integer;
    coreparser : TProfitEval;
begin
  // execute action on On Form Load Event
  ActNode := dbcall.struct.XML.DocumentElement.ChildNodes.FindNode('actions');
  if assigned(ActNode) then
  begin
    for i := 0 to ActNode.ChildNodes.Count-1 do begin
      anode := ActNode.ChildNodes[i];
      if (vartostr(anode.Attributes['apply']) <> 'On Form Load') then continue;
      ASBDataObj.act.actEvent := 'onformload';
      coreparser := ASBDataObj.act.CoreParser;
      ASBDataObj.act.CoreParser := dbCall.Parser;
      ASBDataObj.act.coreparser.OnInitGrid := ASBDataObj.dbcall.InitGrid;
      ASBDataObj.act.coreparser.OnDoFillGrid := ASBDataObj.dbcall.dofillgrid;
      ASBDataObj.act.CoreParser.OnCopyTrans := ASBDataObj.dbcall.CopyTrans;
      ASBDataObj.act.CoreParser.OnCopyTransAndSave := CopyTransAndSave;
      ASBDataObj.act.CoreParser.OnNewTrans := ASBDataObj.dbcall.NewTrans;
      ASBDataObj.act.CoreParser.OnRefreshField := ASBDataObj.dbcall.Validate.RefreshField;
      ASBDataObj.act.root := anode;
      dbm.gf.DoDebug.msg('Action node in structure : ' + ASBDataObj.act.root.XML);
      ASBDataObj.FormLoadingAct := true;
      ASBDataObj.act.execute;
      ASBDataObj.act.CoreParser := coreparser;
    end;
  end;
  ActNode := dbcall.struct.XML.DocumentElement.ChildNodes.FindNode('scripts');
  if assigned(ActNode) then
  begin
    for i := 0 to ActNode.ChildNodes.Count-1 do begin
      anode := ActNode.ChildNodes[i];
      if (vartostr(anode.Attributes['apply']) <> 'On Form Load') then continue;
      ASBDataObj.act.actEvent := 'onformload';
      coreparser := ASBDataObj.act.CoreParser;
      ASBDataObj.act.CoreParser := dbCall.Parser;
      ASBDataObj.act.coreparser.OnInitGrid := ASBDataObj.dbcall.InitGrid;
      ASBDataObj.act.coreparser.OnDoFillGrid := ASBDataObj.dbcall.dofillgrid;
      ASBDataObj.act.CoreParser.OnCopyTrans := ASBDataObj.dbcall.CopyTrans;
      ASBDataObj.act.CoreParser.OnCopyTransAndSave := CopyTransAndSave;
      ASBDataObj.act.CoreParser.OnNewTrans := ASBDataObj.dbcall.NewTrans;
      ASBDataObj.act.CoreParser.OnRefreshField := ASBDataObj.dbcall.Validate.RefreshField;
      ASBDataObj.act.root := anode;
      dbm.gf.DoDebug.msg('Action node in structure : ' + ASBDataObj.act.root.XML);
      ASBDataObj.FormLoadingAct := true;
      ASBDataObj.act.execute;
      ASBDataObj.act.CoreParser := coreparser;
    end;
  end;
end;

procedure TASBTStructObj.FindAndExeDataLoadAction;
    var actnode,anode : ixmlnode;
    i : integer;
    coreparser : TProfitEval;
begin
    // execute action on On Data Load Event
    ActNode := dbcall.struct.XML.DocumentElement.ChildNodes.FindNode('actions');
    if assigned(ActNode) then
    begin
      for i := 0 to ActNode.ChildNodes.Count-1 do begin
        anode := ActNode.ChildNodes[i];
        if (vartostr(anode.Attributes['apply']) <> 'On Data Load') then continue;
        ASBDataObj.act.actEvent := 'ondataload';
        coreparser := ASBDataObj.act.CoreParser;
        ASBDataObj.act.CoreParser := dbCall.Parser;
        ASBDataObj.act.coreparser.OnInitGrid := ASBDataObj.dbcall.InitGrid;
        ASBDataObj.act.coreparser.OnDoFillGrid := ASBDataObj.dbcall.dofillgrid;
        ASBDataObj.act.CoreParser.OnCopyTrans := ASBDataObj.dbcall.CopyTrans;
        ASBDataObj.act.CoreParser.OnCopyTransAndSave := CopyTransAndSave;
        ASBDataObj.act.CoreParser.OnNewTrans := ASBDataObj.dbcall.NewTrans;
        ASBDataObj.act.CoreParser.OnRefreshField := ASBDataObj.dbcall.Validate.RefreshField;
        ASBDataObj.act.root := anode;
        dbm.gf.DoDebug.msg('Action node in structure : ' + ASBDataObj.act.root.XML);
        ASBDataObj.act.execute;
        ASBDataObj.act.CoreParser := coreparser;
      end;
    end;
    // execute scripts on On Data Load Event
    ActNode := dbcall.struct.XML.DocumentElement.ChildNodes.FindNode('scripts');
    if assigned(ActNode) then
    begin
      for i := 0 to ActNode.ChildNodes.Count-1 do begin
        anode := ActNode.ChildNodes[i];
        if (vartostr(anode.Attributes['apply']) <> 'On Data Load') then continue;
        ASBDataObj.act.actEvent := 'ondataload';
        coreparser := ASBDataObj.act.CoreParser;
        ASBDataObj.act.CoreParser := dbCall.Parser;
        ASBDataObj.act.coreparser.OnInitGrid := ASBDataObj.dbcall.InitGrid;
        ASBDataObj.act.coreparser.OnDoFillGrid := ASBDataObj.dbcall.dofillgrid;
        ASBDataObj.act.CoreParser.OnCopyTrans := ASBDataObj.dbcall.CopyTrans;
        ASBDataObj.act.CoreParser.OnCopyTransAndSave := CopyTransAndSave;
        ASBDataObj.act.CoreParser.OnNewTrans := ASBDataObj.dbcall.NewTrans;
        ASBDataObj.act.CoreParser.OnRefreshField := ASBDataObj.dbcall.Validate.RefreshField;
        ASBDataObj.act.root := anode;
        dbm.gf.DoDebug.msg('Action node in structure : ' + ASBDataObj.act.root.XML);
        ASBDataObj.act.execute;
        ASBDataObj.act.CoreParser := coreparser;
      end;
    end;
end;

procedure TASBTStructObj.FindandClearFG(frmName : String);
  var i : integer;
begin
  if not assigned(dbCall) then exit;
  for i:=0 to dbcall.struct.fgs.Count-1 do begin
    if lowercase(frmName) = 'dc' + inttostr(pfg(dbcall.struct.fgs[i]).TargetFrame) then
    begin
       dbm.gf.DoDebug.msg('Clear fillgrid -  '+ pfg(dbcall.struct.fgs[i]).name);
       dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame);
       break;
    end;
  end;
end;

function TASBTStructObj.FindandExecuteFG(frmName,callfrom : String) : boolean;
  var i,j : integer;
  stext : String;
  parambound : boolean;
begin
  result := false;
  if not assigned(dbCall) then exit;
  for i:=0 to dbcall.struct.fgs.Count-1 do begin
    if lowercase(frmName) = 'dc' + inttostr(pfg(dbcall.struct.fgs[i]).TargetFrame) then
    begin
       dbm.gf.DoDebug.msg('Executing fillgrid -  '+ pfg(dbcall.struct.fgs[i]).name);
       stext := '';
       parambound := false;
       if assigned(pfg(dbcall.struct.fgs[i]).q) then
       begin
          if pfg(dbcall.struct.fgs[i]).HasParams = true then
          begin
             parambound := IsFGBound(pfg(dbcall.struct.fgs[i]));
             if not parambound then exit;
          end;
          stext := pfg(dbcall.struct.fgs[i]).q.CDS.CommandText;
       end;
       if pfg(dbcall.struct.fgs[i]).AddRows = 2 then dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame)
       else if pfg(dbcall.struct.fgs[i]).AddRows = 1 then
       begin
          if (pfg(dbcall.struct.fgs[i]).firmbind) and (callfrom = 'getdep&loaddc') then dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame)
          else begin
            if (parambound = false) then
            begin
              j := dbCall.StoreData.GetRowCount(pfg(dbcall.struct.fgs[i]).TargetFrame);
              dbm.gf.DoDebug.msg('Row count : ' + inttostr(j));
              if (j = 1) and (dbcall.Validate.GetRowValidity(dbCall.StoreData.GetRowCount(pfg(dbcall.struct.fgs[i]).TargetFrame),1) = 0) then exit;
              if j > 1 then exit;
            end  else dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame);
          end;
       end;
       dbcall.FillValues(stext,pfg(dbcall.struct.fgs[i]).Map.CommaText,pfg(dbcall.struct.fgs[i]).Groupfield,pfg(dbcall.struct.fgs[i]).SourceFrame) ;
       result := true;
       break;
    end;
  end;
end;

function TASBTStructObj.ExecFillGrid(FrameNo:Integer; callfrom:TfgCall;fgName : String) : pFg;
  var i,j : integer;
  stext : String;
  parambound,VisibleDC: boolean;
begin
  result := nil;
  dbm.gf.dodebug.msg('Executing fill Grid '+IntToStr(FrameNo));
  if not assigned(dbCall) then exit;
  for i:=0 to dbcall.struct.fgs.Count-1 do begin
    if pfg(dbcall.struct.fgs[i]).TargetFrame=FrameNo then begin
       if (CallFrom=fgFromButtonClick) then
       begin
         if pfg(dbcall.struct.fgs[i]).fname <> fgName then continue;
       end;
       if (CallFrom = fgFromFormLoad) then
       begin
          VisibleDC := not ((pos(quotedstr('dc' + inttostr(FrameNo)),VisibleDCs) = 0) and (visibleDCs<>''));
          if (not VisibleDC) or (not pfg(dbcall.struct.fgs[i]).AutoShow) then exit;
       end;
       if (CallFrom = fgFromLoadDC) then
       begin
          if (not pfg(dbcall.struct.fgs[i]).AutoShow) then exit
          else begin
            j := dbCall.StoreData.RowCount(pfg(dbcall.struct.fgs[i]).TargetFrame);
            if (j = 1) and (dbcall.Validate.GetRowValidity(pfg(dbcall.struct.fgs[i]).TargetFrame,1) = 1) then exit;
            if j > 1 then exit;
          end;
       end;
       if assigned(pfg(dbcall.struct.fgs[i]).q) then begin
         if (dbCall.struct.quickload) and (CallFrom = fgFromDependents) then
             parambound := true
         else if (dbCall.struct.quickload) and (CallFrom = fgFromButtonClick) then
             parambound := true
         else if (dbCall.struct.quickload) and (CallFrom = fgFromLoadDC) then
             parambound := true
         else parambound := IsFGBound(pfg(dbcall.struct.fgs[i]));
         if (not parambound) and (CallFrom<>fgFromButtonClick) then exit;
       end;
       if pfg(dbcall.struct.fgs[i]).AddRows = 2 then dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame)
       else if pfg(dbcall.struct.fgs[i]).AddRows = 1 then
       begin
          if (pfg(dbcall.struct.fgs[i]).firmbind) and (callfrom <> fgFromFormLoad) then dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame)
          else begin
            if (parambound = false) then
            begin
              j := dbCall.StoreData.RowCount(pfg(dbcall.struct.fgs[i]).TargetFrame);
              if (j = 1) and (dbcall.Validate.GetRowValidity(pfg(dbcall.struct.fgs[i]).TargetFrame,1) = 1) then exit;
              if j > 1 then exit;
            end  else dbcall.InitGrid(pfg(dbcall.struct.fgs[i]).TargetFrame);
          end;
       end;
       dbcall.FillGrid(pfg(dbcall.struct.fgs[i])) ;
       result := pfg(dbcall.struct.fgs[i]);
       if dbcall.StoreData.GetRowCount(FrameNo) > 0 then
          dbcall.Struct.GetFrame(FrameNo).HasDataRows := True;
       break;
    end;
  end;
end;

function TASBTStructObj.IsFGBound(fg:pfg):boolean;
var i:integer;
    s,v:String;
    f:pfld;
begin
  result:=true;
  i:=1;
  dbm.gf.dodebug.msg('Fii Grid Name : ' + fg.fname);
  dbm.gf.dodebug.msg('Fii Grid Params : ' + fg.paramnames);
  while True do begin
    s:=dbm.gf.getnthstring(fg.paramnames, i);
    v := '';
    if s='' then break;
    If dbcall.struct.oldfields.IndexOf(s) > -1 Then Begin
       inc(i);
       continue;
    End;
    f:=dbcall.struct.getfield(s);
    if assigned(f) then
    begin
      v := dbcall.StoreData.GetFieldValue(s,1);
      if dbcall.validate.EmptyCheck(f, 1) then begin
        result:=false;
//        break;
      end;
    end else begin
      v := dbcall.validate.Parser.GetVarValue(s);
      if v = '' then begin
        result:=false;
//        break;
      end;
    end;
    dbm.gf.dodebug.msg('Param Name : ' + s + ' Value : ' + v);
    inc(i);
  end;
end;

procedure TASBTStructObj.WriteResultForGrid(q:TXDS;rnode,fgnode : ixmlnode;fs,colsize:String);
var i,j:integer;
    row,n : IXMLNode;
    fgSourceSQLFldName,fgTargetStructFldName,sFldType,sFldValue : String;
    tmpPfld : pFld;
    fgMap : TStringList;
begin
  try
    tmpPfld := nil;
    fgMap := nil;
    if (q.CDS.RecordCount = 0) then exit;
    dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
    (*
    fgMap StringList object introduced to hold fillgrid mapping.
    It could be taken from fillgrid object's map Tstringlist (fg.map) but in the map Stringlist
    we have stored data like StructFieldName (Target) = SQLFieldName (Source).
    But here we required data in Source = Target format for easy access.
    So we loaded temporary map stringlist object when creating header row details itself.
    *)
    fgMap := TStringList.Create;
    fgMap.StrictDelimiter := true;
    fgnode := fgnode.ChildNodes.FindNode('a7');
    row := rnode.addchild('headrow');
    if fs <> '' then row.Attributes['tlhw'] := fs;
    //Adding head row node
    for i:=0 to q.CDS.FieldCount-1 do begin
      fgSourceSQLFldName := q.CDS.Fields[i].FieldName;
      xnode := row.addchild(fgSourceSQLFldName);
      xnode.Attributes['width'] := dbm.gf.getnthstring(colsize,i+2);
      xnode.text := fgSourceSQLFldName;
      n := fgnode.ChildNodes.FindNode(lowercase(fgSourceSQLFldName));
      if n = nil then n:= fgnode.ChildNodes.FindNode(uppercase(fgSourceSQLFldName));
      if assigned(n) then
      begin
        if n.HasAttribute('cap') then
           if vartostr(n.Attributes['cap']) <> '' then xnode.text := n.Attributes['cap'];
        fgMap.Add(fgSourceSQLFldName+'='+n.NodeName);
      end else
      begin
         for j := 0 to fgnode.ChildNodes.Count - 1 do
         begin
           if lowercase(fgnode.ChildNodes[j].NodeValue) = lowercase(fgSourceSQLFldName) then
           begin
              if fgnode.ChildNodes[j].HasAttribute('cap') then
                 if vartostr(fgnode.ChildNodes[j].Attributes['cap']) <> '' then xnode.text := fgnode.ChildNodes[j].Attributes['cap'];
              fgMap.Add(fgSourceSQLFldName+'='+fgnode.ChildNodes[j].NodeName);
              break;
           end ;
         end;
      end;
    end;

    q.CDS.First;
    while not q.CDS.Eof do begin
      row := rnode.addchild('row');
      for i:=0 to q.CDS.FieldCount-1 do begin
        tmpPfld:= nil;
        fgTargetStructFldName := '';

        fgSourceSQLFldName := q.CDS.Fields[i].FieldName; //read fieldname from cds
        sFldType := 'c'; //default set to 'c' / character type
        xnode := row.addchild(fgSourceSQLFldName);

        (*
        Get Target struct field name from mapping details.
        By default, the CaseSensitive property of StringList (tmpfg.map) is set to False,
        so, no need worry about casesensitve data.
        *)
        fgTargetStructFldName := fgMap.values[fgSourceSQLFldName];

        (*
        Get actual field's (target fields) fieldtype from the structdef.
        dbcall is getting assigned in the main functionality , so assigned of check is
        removed. It can be added if its required.
        //if Assigned(dbCall) then
        *)
        if (fgTargetStructFldName <> '') then
        begin
          tmpPfld := dbcall.struct.GetField(fgTargetStructFldName);
          if Assigned(tmpPfld) then
            sFldType := tmpPfld.DataType;
        end;
        sFldValue := q.CDS.Fields[i].AsString;
        (*
          When we read numeric data from the clientdataset, it loses the trailing 0.
          For ex:
          if the actual data is 123.40 when we read from the cds it returns 123.4.
          Based on CDS field datatype (ftFloat), We tried to read as AsFloat value and checked, but
          it didnt work. (Tried by read as variant , but didnt work)
          To overcome this issue, we used Axpert's StructDef object to get field (target field) type and called
          FormatNumber function for Numeric field datatype to format the number.
        *)
        if sFldType = 'n' then
          sFldValue := dbcall.Validate.FormatNumber(fgTargetStructFldName,sFldValue);
        xnode.text := sFldValue;
      end;
      q.CDS.next;
    end;
  finally
    tmpPfld := nil;
    if Assigned(fgMap) then
      FreeAndNil(fgMap);
  end;
end;

procedure TASBTStructObj.MakeResponse(v : String ; mn,enode : ixmlnode);
  var n,n1,rnode : ixmlnode;
  q : TXDS;
begin
  rnode := mn.ChildNodes.FindNode('response') ;
  rnode := rnode.ChildNodes.FindNode('dep') ;  // to fill dep node for dofillfridvalue services
  if rnode = nil then
  begin
    rnode := mn.ChildNodes.FindNode('response') ;
    rnode := rnode.ChildNodes.FindNode('fieldvalue') ;  // to fill values for setvalue action
  end;
  if rnode = nil then rnode := mn.ChildNodes.FindNode('response') ;
  q:=fld.QSelect;
  if assigned(q) then begin
    n := rnode.AddChild(fld.FieldName);
    if v <> '' then n.Attributes['value'] := v;
    if fld.SourceKey then n.Attributes['idcol'] := 'yes'
    else n.Attributes['idcol'] := '';
    if (fld.ModeofEntry = 'accept') or (fld.txtSelection = true) then
    begin
      n.Attributes['ntype'] := 'single' ;
      n.NodeValue := v;
    end else
    begin
      n.Attributes['ntype'] := 'multiple';
      n.Attributes['value'] := v;
      writeresult(q,n);
    end;
  end else
  begin
    n := rnode.AddChild(fld.FieldName);
    n.Attributes['ntype'] := 'single';
    n.NodeValue := v;
  end;
  n1 := enode.ChildNodes.FindNode(fld.FieldName);
  if n1 <> nil then n1 := n1.ChildNodes.FindNode('a31');
  if n1 <> nil then
  begin
     if n1.HasAttribute('ctype') then n.Attributes['ctype'] := vartostr(n1.Attributes['ctype']);
  end;
end;

procedure TASBTStructObj.WriteResult(q:TXDS;rnode : ixmlnode);
var i:integer;
    row: IXMLNode;
begin
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
  if q.CDS.RecordCount = 0 then exit;
  q.CDS.First;
  while not q.CDS.Eof do begin
    row := rnode.addchild('row');
    for i:=0 to q.CDS.FieldCount-1 do begin
      if lowercase(q.CDS.Fields[i].FieldName) = 'axrnum' then continue;   // to skip axrnum column which is used for pagination.
      xnode := row.addchild(q.CDS.Fields[i].FieldName);
      if q.CDS.fields[i].text = '' then
        xnode.Text := '*'
      else
      xnode.text := q.CDS.Fields[i].AsString;
    end;
    q.CDS.next;
  end;
end;

function TASBTStructObj.InsertCondition(sqltext,val : String) : String ;
   var i,j,k: integer;
       c,lastword,brackets,tmpSql,orgSql,ordbyclouse,grpbyclouse : String;
       whereClouse : Boolean;
begin
   result := '';
    lastword:='';
    brackets:='';
    ordbyclouse := '';
    grpbyclouse := '';
    whereClouse := False;
    if pos('where',lowercase(sqltext)) > 0  then
    begin
      dbm.gf.dodebug.msg('Changing where condition');
      whereClouse := true;
      for i:=1 to length(sqltext) do
      begin
        c:=sqltext[i];
        if (c = ' ') or (c = char(13)) or (c = '(') or (c = ')') or (c = ',') then
        begin
          dbm.gf.dodebug.msg('Last Word : ' + lastword);
          if c='(' then brackets:=brackets+'(';
          if c=')' then delete(brackets, length(brackets), 1);
          if (trim(lowercase(lastword))='where') and (length(brackets)=0) then
          begin
            sqltext := copy(sqltext,1,i) + ' ' + val + ' ' + copy(sqltext,i,length(sqltext));
          end;
          lastword:='';
          c := '';
        end;
        lastword:=lastword+c;
      end;
      orgSql := sqltext;
    end;
    if not whereClouse then
    begin
      tmpSql := lowercase(sqltext);
      orgSql := '';
      j := 0;
      delete(val,length(val)-3,6);
      val := ' where ' + val;
      while True do
      begin
        i := pos('order by ', lowercase(sqltext));
        if (i>0) then begin
          orgSql := orgSql + copy(sqltext,1,i-1);
          delete(sqltext,1,i-1);
          sqltext := trim(sqltext);
          ordbyclouse := copy(sqltext,1,9);
          delete(sqltext,1,8);
          sqltext := trim(sqltext);
          j := pos(' ',sqltext);
          if j > 0 then
             ordbyclouse := ordbyclouse + copy(sqltext,1,j)
          else  ordbyclouse := ordbyclouse + sqltext;
        end else if (pos('group by ', lowercase(sqltext))>0) then begin
          i := pos('group by ', lowercase(sqltext));
          orgSql := orgSql + copy(sqltext,1,i-1);
          delete(sqltext,1,i-1);
          sqltext := trim(sqltext);
          grpbyclouse := copy(sqltext,1,9);
          delete(sqltext,1,8);
          sqltext := trim(sqltext);
          j := pos(' ',sqltext);
          if j > 0 then
             grpbyclouse := grpbyclouse + copy(sqltext,1,j)
          else  grpbyclouse := grpbyclouse + sqltext;
        end else if (pos('union ', lowercase(sqltext))>0) then begin
          i := pos('union ', lowercase(sqltext));
          orgSql := orgSql + copy(sqltext,1,i-1) + val;
          delete(sqltext,1,i-1);
          k := pos('select ',lowercase(sqltext));
          orgSql := orgSql + copy(sqltext,1,k-1);
          delete(sqltext,1,k-1);
        end else
        begin
          if grpbyclouse <> '' then
             orgSql := orgSql + val + grpbyclouse;
          if ordbyclouse <> '' then
             orgSql := orgSql + val + ordbyclouse
          else orgSql := orgSql + sqltext + val;
          break;
        end;
        if i = 0 then break;
      end;
    end;
    result := orgSql;
end;

function TASBTStructObj.AssignParams(Q:TXDS) : boolean ;
var i,j,k, rc :integer;
    n,t,v, cValue, LastDataType, val:String;
    rnode : ixmlnode;
    fld1 : pFld;
begin
  result := true;
  dbm.gf.dodebug.msg('Assigning Params');
  rnode := xmldoc.DocumentElement;
  if not assigned(q.CDS.params) then exit;
  dbm.gf.dodebug.msg('Param sql : ' + q.CDS.CommandText);
  if pos(':',q.CDS.CommandText) <> 0 then
  begin
    if q.CDS.params.count > 0 then
    begin
      for i:=0 to q.CDS.params.count-1 do begin
        n := q.CDS.params[i].Name;
        v := dbcall.Parser.GetVarValue(n);
        q.AssignParam(i,v,dbcall.Parser.LastVarType);
        dbm.gf.dodebug.Msg(n+'['+dbcall.Parser.LastVarType+']='+v);
      end;
    end;
  end ;
  if pos('{',q.CDS.CommandText) <> 0 then
  begin
      while pos('{',q.CDS.CommandText) > 0 do
      begin
        n := q.CDS.CommandText;
        i := pos('{',n) + 1;
        j := pos('}',n)  - i;
        t := copy(n,i,j);
        dbm.gf.dodebug.Msg('Param Name :' + t) ;

        if copy(t,Length(t),1) = '*' then
        begin
          Delete(t,Length(t),1);
          t := trim(t);
          fld1 := dbcall.struct.GetField(t);
          if pfrm(dbcall.struct.frames[fld1.FrameNo-1]).AsGrid then
          begin
            rc := dbcall.Storedata.GetRowCount(fld1.FrameNo);
            cValue := '';
            for k := 1 to rc do
            begin
              val := dbcall.Storedata.GetFieldValue(t, k);
              if (k = 1) then
                LastDataType := uppercase(fld1.DataType);
              If (LastDataType = 'N') then begin
                if (val = '') Then val := '0' else val := AxProvider.dbm.gf.RemoveCommas(val);
                cValue := cValue+','+ val;
              end else
                cValue := cValue+','+ quotedstr(val);
            end;
            Delete(cValue,1,1);
            if cValue = '' then
            begin
              if fld1.DataType = 'n' then
                cvalue := '0'
              else
                cValue := QuotedStr(cValue);
            end;
    //        if cValue = '' then cValue := quotedstr('');
          end
          else
            CValue := dbcall.Parser.GetVarValue(t);
          t := cValue;
        end
        else
          t := dbcall.Parser.GetVarValue(t);

        dbm.gf.dodebug.Msg('Param Value :' + t) ;
        v := copy(n,i-1,j+2);
        n := dbm.gf.FindAndReplace(n,v,t);
        dbm.gf.dodebug.Msg('Replace with :' + v) ;
        q.CDS.CommandText := n;
        dbm.gf.dodebug.Msg('Sql :' + n) ;
        if t = '' then result := false;
      end;
  end;
end;

procedure TASBTStructObj.WriteResultForGetFieldValue(q:TXDS ; rnode : ixmlnode;dynamicfilter : boolean ;map : AnsiString);
var i,j, k , m :integer;
    row : IXMLNode;
    name,val,cw : String;
    fld1 : pFld;
    found : boolean;
    sl1,sl2 : TStringList;
begin
  sl1 := nil; sl2 := nil;
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
  if q.CDS.RecordCount = 0 then exit;
  q.CDS.First;
  while not q.CDS.Eof do begin
    try
      row := rnode.addchild('row');
      k := 2;
      for i:=0 to q.CDS.FieldCount-1 do begin
        if lowercase(q.CDS.Fields[i].FieldName) = 'axrnum' then continue;   // to skip axrnum column which is used for pagination.
        name := q.CDS.Fields[i].FieldName;
        if assigned(fld) then
        begin
          if fld.SourceKey  then
          begin
            if i = 0 then
              cw := '0'
            else begin
              cw := dbm.gf.GetNthString(fld.sgwidth,k);
              inc(k);
            end;
            if i = 1 then
            begin
              if map <> '' then name := map
              else  name := fld.FieldName;
              if name = '' then name := fld.FieldName;
            end;
          end else begin
            if i = 0 then
            begin
              if map <> '' then name := map
              else  name := fld.FieldName;
              if name = '' then name := fld.FieldName;
            end;
            cw := dbm.gf.GetNthString(fld.sgwidth,k);
           inc(k);
          end;
        end;
        if q.CDS.fields[i].text = '' then
          val := '*'
        else
          val := q.CDS.Fields[i].AsString;
        fld1 := nil;
        found := false;
        if (fld.SourceKey and (i > 1))  or (not fld.SourceKey and (i > 0)) then
        begin
          if assigned(dbCall) then
          begin
            for j:=0 to dbcall.struct.flds.count-1 do
            begin
               fld1:=dbcall.struct.flds[j];
               if (lowercase(name) = lowercase(fld1.SourceField)) and (fld1.LinkField = fld.FieldName) then
               begin
                 if fld1.DataType = 'n' then
                 begin
                   if val <> '*' then val := dbcall.Validate.FormatNumber(fld1.FieldName,val);
                 end;
                 found := true;
                 break;
               end else fld1 := nil;
            end;
          end;
        end;
        if (fld.SourceKey and (i <= 1))  or (not fld.SourceKey and (i = 0)) then
        begin
           xnode := row.addchild(name);
           xnode.Attributes['w'] := cw;
           if (fld.SourceKey) and (i=0) then xnode.Attributes['cap'] := ''
           else if fld.Caption <> '' then xnode.Attributes['cap'] := fld.Caption
           else xnode.Attributes['cap'] := fld.FieldName;
           if fld.hidden then xnode.Attributes['hidden'] := 'T'
           else xnode.Attributes['hidden'] := 'F';
           xnode.text := val;
        end else if found and assigned(fld1) then
        begin
           //if (not fld1.hidden) then
           //begin
             xnode := row.addchild(name);
             xnode.Attributes['w'] := cw;
             if fld1.Caption <> '' then  xnode.Attributes['cap'] := fld1.Caption
             else xnode.Attributes['cap'] := fld1.FieldName;
             if fld1.hidden then xnode.Attributes['hidden'] := 'T'
             else xnode.Attributes['hidden'] := 'F';
             xnode.text := val;
           //end;
        end else if dynamicfilter then
        begin
            if assigned(fld.pickfields) then
            begin
              if not assigned(sl1) then
              begin
                sl1 := TStringList.Create;
                sl1.CommaText := fld.pickfields.Strings[0];
              end;
              if assigned(fld.pickcaptions) then
              begin
                if not assigned(sl2) then
                begin
                  sl2 := TStringList.Create;
                  sl2.CommaText := fld.pickcaptions.Strings[0];
                end;
              end;
              for m := 0 to sl1.Count - 1 do
              begin
                 if pos(lowercase(name),lowercase(sl1.Strings[m])) > 0  then
                 begin
                   xnode := row.addchild(name);
                   xnode.Attributes['w'] := cw;
                   if assigned(fld.pickcaptions) and (i<=sl2.Count-1) then
                      xnode.Attributes['cap'] := sl2.Strings[m]
                   else xnode.Attributes['cap'] := name;
                   if fld.hidden then xnode.Attributes['hidden'] := 'T'
                   else xnode.Attributes['hidden'] := 'F';
                   xnode.text := val;
                   break;
                 end;
              end;
            end;
        end;
      end;
    except
    end;
    q.CDS.next;
  end;
  if assigned(sl1) then
  begin
     sl1.Clear;
     sl1.Free;
  end;
  if assigned(sl2) then
  begin
     sl2.Clear;
     sl2.Free;
  end;
end;

procedure TASBTStructObj.WriteSQLResultForGetFieldValue(q:TXDS ; rnode : ixmlnode;dynamicfilter : boolean;map : AnsiString);
var i,j, k , m :integer;
    row : IXMLNode;
    name,val,cw : String;
    fld1 : pFld;
    found : boolean;
begin
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
  if q.CDS.RecordCount = 0 then exit;
  q.CDS.First;
  while not q.CDS.Eof do begin
    row := rnode.addchild('row');
    k := 2;
    for i:=0 to q.CDS.FieldCount-1 do begin
      if lowercase(q.CDS.Fields[i].FieldName) = 'axrnum' then continue;   // to skip axrnum column which is used for pagination.
      name := q.CDS.Fields[i].FieldName;
      if q.CDS.fields[i].text = '' then
        val := '*'
      else
        val := q.CDS.Fields[i].AsString;
      fld1 := nil;
      found := false;
      if assigned(fld) then
      begin
        if fld.SourceKey  then
        begin
          if i = 0 then
            cw := '0'
          else begin
            cw := dbm.gf.GetNthString(fld.sgwidth,k);
            inc(k);
          end;
          if i = 1 then
          begin
            if map <> '' then name := map
            else  name := fld.FieldName;
          end;
        end else begin
          if i = 0 then
          begin
            if map <> '' then name := map
            else  name := fld.FieldName;
          end;
          cw := dbm.gf.GetNthString(fld.sgwidth,k);
         inc(k);
        end;
        if (fld.SourceKey and (i > 1))  or (not fld.SourceKey and (i > 0)) then
        begin
          if assigned(dbCall) then
          begin
            for j:=0 to dbcall.struct.flds.count-1 do
            begin
               fld1:=dbcall.struct.flds[j];
               if (lowercase(name) = lowercase(fld1.SourceField)) and (fld1.LinkField = fld.FieldName) then
               begin
                 if fld1.DataType = 'n' then
                 begin
                   if val <> '*' then val := dbcall.Validate.FormatNumber(fld1.FieldName,val);
                 end;
                 found := true;
                 break;
               end else fld1 := nil;
            end;
          end;
        end;
        {
        if (fld.SourceKey and (i <= 1))  or (not fld.SourceKey and (i = 0)) then
        begin
           xnode := row.addchild(name);
           xnode.Attributes['w'] := cw;
           if (fld.SourceKey) and (i=0) then xnode.Attributes['cap'] := ''
           else if fld.Caption <> '' then xnode.Attributes['cap'] := fld.Caption
           else xnode.Attributes['cap'] := fld.FieldName;
           xnode.text := val;
        end;
        }
      end;
      if found and assigned(fld1) then
      begin
         xnode := row.addchild(name);
         xnode.Attributes['w'] := cw;
         if fld1.Caption <> '' then  xnode.Attributes['cap'] := fld1.Caption
         else xnode.Attributes['cap'] := fld1.FieldName;
         xnode.text := val;
      end else
      begin
         xnode := row.addchild(name);
         xnode.Attributes['w'] := cw;
         xnode.Attributes['cap'] := name;
         xnode.text := val;
      end;
    end;
    q.CDS.next;
  end;
end;

function TASBTStructObj.GetActiveRowDepentendList(fname : String ) : String ;
  var n : ixmlnode;
      dList : String;
begin
    result := '';
    dbm.gf.dodebug.msg('Active row dependency for : ' + fname);
    n :=dbcall.struct.XML.DocumentElement.ChildNodes.FindNode(fname);
    if (n <> nil) then
    begin
      dlist := vartostr(n.Attributes['rdf']);
      dbm.gf.dodebug.msg('Dep Field List: ' + dlist);
    end;
    if dlist <> '' then
    begin
      dlist := dlist + ',';
    end;
    result := dlist;
    dbm.gf.dodebug.msg('Active row dependency list : ' + result);
end;

Procedure TASBTStructObj.DeleteInvalidPopGridRows;
  var j,pidx : integer;
begin
  for j:=0 to dbcall.struct.flds.Count-1 do
  begin
    fld:=dbcall.struct.flds[j];
    if pfrm(dbcall.struct.frames[fld.FrameNo-1]).popindex > -1 then
    begin
      pidx:=pfrm(dbcall.struct.frames[fld.FrameNo-1]).popindex;
      if (assigned(pPopGrid(dbcall.struct.popgrids[pidx]).AutoFill)) and (pPopGrid(dbcall.struct.popgrids[pidx]).FirmBind)  then
      begin
         DbCall.Validate.DeletePopRows(fld.FrameNo);
      end;
    end;
  end;
end;

function TASBTStructObj.SaveTrans():String;
var flag : Boolean;
    f,v,tname,iname : String;
    n : ixmlnode;
    i : integer;
    recid : Extended;
begin
  result := '';
  axprovider.dbm.gf.dodebug.msg('Executing SaveTrans.');
  flag := false;
  try
    if assigned(WithDbCall) then begin
      flag := axprovider.dbm.InTransaction;
      if not flag then axprovider.dbm.StartTransaction(axprovider.dbm.gf.connectionname);
      WithDbCall.SaveData;
      if not flag then Axprovider.dbm.Commit(Axprovider.dbm.gf.connectionname);
    end else if assigned(dbcall) then begin
      if dbcall.ValidateData <> '' then begin
        raise exception.Create(DbCall.ErrorStr);
      end;
      flag := axprovider.dbm.InTransaction;
      if not flag then axprovider.dbm.StartTransaction(axprovider.dbm.gf.connectionname);
      dbcall.SaveData;
      if not flag then Axprovider.dbm.Commit(Axprovider.dbm.gf.connectionname);
      f:=dbcall.storedata.primarytablename+'id';
      v:=floattostr(dbcall.LastSavedId);
      recid := dbcall.GetRecordId(f,v);
      for i:=0 to dbcall.struct.flds.count-1 do begin
        fld := pfld(dbcall.struct.flds[i]);
        tname := '';
        iname := '';
        if fld.DataType = 'i' then
        begin
          if trim(dbcall.struct.SchemaName) <> '' then
            tname := dbcall.struct.SchemaName + '.' + dbcall.transid+trim(fld.FieldName)
          else
            tname := dbcall.transid+trim(fld.FieldName);
          f := fld.FieldName;
          n := xnode.ChildNodes.FindNode(f) ;
          if assigned(n) then iname := vartostr(n.NodeValue);
          if (tname <> '') and (iname <> '') then
          begin
            if not axprovider.SaveImage(tname,iname,recid) then
            begin
               raise exception.Create('Could not save image...');
            end;
          end;
        end;
        dbm.gf.dodebug.msg('Transaction commited');
      end;
    end;
  except
    On E:Exception do begin
      if assigned(dbm.Connection) then
      begin
        if not flag then axprovider.dbm.RollBack(Axprovider.dbm.gf.connectionname);
      end;
      result := E.Message;
      Raise Exception.Create(E.Message);
    end;
  end;
end;

function TASBTStructObj.EndTrans():String;
begin
  result := '';
  axprovider.dbm.gf.dodebug.msg('Executing EndTrans.');
  if assigned(WithDbCall) then begin
    WithDbCall.Destroy;
    WithDbCall := nil;
    DbCall.Validate.Parser.WithTransStoreData := nil;
  end;
end;

function TASBTStructObj.LoadTrans(Transid:String;Recordid:Extended):String;
begin
  result := '';
  axprovider.dbm.gf.dodebug.msg('Executing LoadTrans.');
  try
  if assigned(WithDbCall) then
    Raise Exception.Create('Exiting WithTransaction is not yet closed.')
  else begin
    if (Transid = '') then
       Raise Exception.Create('Transid is empty in WithTransaction call.');
    if (Recordid = 0) then
       Raise Exception.Create('Recordid is 0 in WithTransaction call.');
    WithDbCall := TDbCall.create;
    WithDbCall.Dbm := axprovider.dbm;
    WithDbCall.axprovider := axprovider;
    WithDbCall.transid := Transid;
    WithDbCall.LoadData(Recordid);
    DbCall.Validate.Parser.WithTransStoreData := WithDbCall.StoreData;
  end;
  except
    On E:Exception do begin
      result := E.Message;
      if assigned(WithDbcall) then begin
        WithDbCall.Destroy;
        WithDbCall := nil;
      end;
      axprovider.dbm.gf.dodebug.msg('Error in LoadTrans :'+E.Message);
      Raise Exception.Create(E.Message);
    end;
  end;
end;

function TASBTStructObj.SubmitAndValidateInWFAction( x : String) : String;
var f,v,tcstr:string;
    i:integer;
begin
    result := '';
    try
      tcstr := ASBDataObj.CheckForAccess(dbcall.transid);
      if (tcstr = 'e') or (tcstr = 'd') or (tcstr = '') then begin
        dbm.gf.dodebug.msg('Submitting data to dbcall');
        xnode := xmldoc.DocumentElement.ChildNodes.FindNode('data');
        for i:=0 to xnode.ChildNodes.Count-1 do begin
          f := xnode.ChildNodes[i].NodeName;
          fld:=dbcall.struct.GetField(f);
          if not assigned(fld) then continue;
          v := xnode.ChildNodes[i].Text;
          dbcall.SubmitValue(f, v, '', 0, 0, 0);
        end;
        dbm.gf.dodebug.msg('Starting database transaction for validating & saving');
        dbm.gf.dodebug.msg('DB Call Validate and save');
        dbcall.ValidateData;
        if dbcall.errorstr <> '' then begin
          dbm.gf.dodebug.msg('DB Call error msg : ' +dbcall.ErrorStr );
          raise exception.Create(dbcall.ErrorStr);
        end;
      end else result := 'Access denied for this transaction';
  except on E : Exception do
    begin
      result:= e.Message;
      dbm.gf.dodebug.Msg(e.Message);
    end;
  end;
end;

function TASBTStructObj.SaveDataInWFAction : String;
var f,v,tcstr,tname,iname:String;
    i:integer;
    n : ixmlnode;
    recid : extended;
begin
    result := '';
    try
      tcstr := ASBDataObj.CheckForAccess(dbcall.transid);
      if (tcstr = 'e') or (tcstr = 'd') or (tcstr = '') then begin
        dbm.gf.dodebug.msg('Starting database transaction saving');
        dbm.gf.dodebug.msg('DB Call Validate and save');
        dbcall.CallFromSaveDataInWFAction := True;
        dbcall.SaveData;
        dbcall.CallFromSaveDataInWFAction := False;
        f:=dbcall.storedata.primarytablename+'id';
        v:=floattostr(dbcall.LastSavedId);
        recid := dbcall.GetRecordId(f,v);
        for i:=0 to dbcall.struct.flds.count-1 do begin
          fld := pfld(dbcall.struct.flds[i]);
          tname := '';
          iname := '';
          if fld.DataType = 'i' then
          begin
            if trim(dbcall.struct.SchemaName) <> '' then
              tname := dbcall.struct.SchemaName + '.' + dbcall.transid+trim(fld.FieldName)
            else
              tname := dbcall.transid+trim(fld.FieldName);
            f := fld.FieldName;
            n := xnode.ChildNodes.FindNode(f) ;
            if assigned(n) then iname := vartostr(n.NodeValue);
            if (tname <> '') and (iname <> '') then
            begin
              if not axprovider.SaveImage(tname,iname,recid) then
              begin
                 raise exception.Create('Could not save image...');
              end;
            end;
          end;
          dbm.gf.dodebug.msg('Transaction commited');
        end;
      end else result := 'Access denied for saving this transaction';
  except on E : Exception do
    begin
      result:= e.Message;
      dbm.gf.dodebug.Msg(e.Message);
    end;
  end;
end;

procedure TASBTStructObj.WriteResultWithCap(q:TXDS;rnode : ixmlnode);
var i:integer;
    row,xnode : IXMLNode;
    name,cap,s : String;
    fld : pFld;
begin
  if q.CDS.RecordCount = 0 then exit;
  q.CDS.First;
  while not q.CDS.Eof do begin
    row := rnode.addchild('row');
    for i:=0 to q.CDS.FieldCount-1 do begin
      name := q.CDS.Fields[i].FieldName;
      if lowercase(name) = 'axrnum' then continue;   // to skip axrnum column which is used for pagination.
      cap := '';
      xnode := row.addchild(name);
      fld := nil;
      if assigned(dbCall) then
      begin
        fld:=dbcall.struct.GetField(name);
        if assigned(fld) then cap := fld.Caption;
        xnode.Attributes['cap'] := cap;
      end;
      s := q.CDS.fields[i].text;
      if (assigned(fld)) and (fld.DataType = 'n')and (s <> '') then
      begin
         s := dbcall.Validate.FormatNum(s, fld.Width, fld.Dec);
      end;
      if s = '' then
        xnode.Text := ''
      else
        xnode.text := s;
    end;
    q.CDS.next;
  end;
end;

Procedure TASBTStructObj.PopupAutoFill(FldName,OldVal,NewVal:String;popindex,rowno:integer);
var sQry:TXDS;
    popgrid : pPopGrid;
begin
  popgrid := pPopgrid(DbCall.struct.popgrids[popindex]);
  sQry := Axprovider.dbm.GetXDS(nil);
  sQry.buffered := True;
  sQry.CDS.CommandText := popGrid.AutoFill.Text;
  DbCall.Validate.DynamicSQL := sQry.CDS.CommandText;
  DbCall.Validate.QueryOpen(sQry,RowNo);
  DeleteExistingRows(FldName,OldVal,NewVal,popindex,rowno,popgrid);
  CopyQResult(sQry,popgrid);
  sQry.close; FreeAndNil(sQry);
end;

Procedure TASBTStructObj.DeleteExistingRows(FldName,OldVal,NewVal:String;
          popindex,rowno:integer;popgrid:pPopgrid);
var i,ind : integer;
    fname : String;
begin
  DbCall.GetParentValue(popindex,RowNo,DbCall.ParentList);
  if FldName = '' then begin
    DbCall.ActualRows.CommaText := dbcall.GetActualRows(popindex,DbCall.ParentList);
    for i := DbCall.ActualRows.Count-1 downto 0 do begin
      DbCall.Storedata.DeleteRow(popgrid.FrameNo,StrToInt(DbCall.ActualRows[i]));
    end;
  end else begin
    fname := 'sub'+Trim(IntToStr(popgrid.FrameNo))+'_'+Fldname;
    ind := DbCall.ParentList.IndexOfName(fname);
    if ind > -1 then
      DbCall.ParentList[ind] := fname+'='+OldVal;
    DbCall.ActualRows.CommaText := DbCall.GetActualRows(popindex,DbCall.ParentList);
    for i := DbCall.ActualRows.Count - 1 downto 0 do
    begin
      DbCall.StoreData.DeleteRow(popgrid.FrameNo,strToInt(DbCall.ActualRows[i]));
    end;
    if ind > -1 then
      DbCall.ParentList[ind] := fname+'='+NewVal;
    DbCall.ActualRows.CommaText := DbCall.GetActualRows(popindex,DbCall.ParentList);
  end;
end;

Procedure TASBTStructObj.GetParentValue(fldName : String; fno,rno : integer);
  var newpval : String;
begin
    newpval := dbcall.StoreData.GetFieldValue(fldName,rno);
    if oldpval <> newpval then
    begin
        if fldName <> '' then DbCall.StoreData.SubmitValue(fldName, rno, oldpval, '', 0, 0, 0);
        dbcall.GetParentValue(fno,rno,dbcall.ParentList);
        if fldName <> '' then DbCall.StoreData.SubmitValue(fldName, rno, newpval, '', 0, 0, 0);
    end else if fldName <> '' then dbcall.GetParentValue(fno,rno,dbcall.ParentList);
end;

Procedure TASBTStructObj.CopyQResult(q:TXDS;popgrid:pPopgrid);
var i,rno : integer;
    tfld : pfld;
    fname : String;
begin
  rno := DbCall.StoreData.GetRowCount(popgrid.FrameNo);
  DbCall.ActualRows.Clear;
  while not q.CDS.Eof do begin
    inc(rno);
    DbCall.ActualRows.Add(IntToStr(rno));
    for i := 0 to q.CDS.FieldCount - 1 do begin
      tfld := DbCall.struct.GetField(q.CDS.Fields[i].FieldName);
      if tfld = nil then
        Raise Exception.Create(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(DbCall.struct.frames[popGrid.FrameNo-1]).Caption);
      if tfld.FrameNo <> popgrid.FrameNo then
        Raise Exception.Create(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(DbCall.struct.frames[popGrid.FrameNo-1]).Caption);
      DbCall.StoreData.SubmitValue(tfld.FieldName,rno,q.CDS.Fields[i].AsString,'',0,0,0);
      InsertParentDetails(rno);
    end;
    if popgrid.AutoFillFld then begin
      fname := 'sub'+Trim(IntToStr(popgrid.frameno))+'_autofill';
      tfld := DbCall.struct.GetField(fname);
      if tfld <> nil then
        fname := tfld.FieldName;
      DbCall.StoreData.SubmitValue(fname,rno,'T','',0,0,0);
    end;
    q.CDS.Next;
  end;
end;

Procedure TASBTStructObj.InsertParentDetails(RowNo:integer);
var i : integer;
begin
  For i := 0 to DbCall.ParentList.Count-1 do
    DbCall.Storedata.submitvalue(DbCall.ParentList.Names[i], Rowno, DbCall.ParentList.ValueFromIndex[i],'',0,0,0);
end;


function TASBTStructObj.CreateErrNode(errmsg : String ; xmlresult : boolean) : String;
begin
  if xmlresult then
  begin
    result := '<error>' + errmsg + '</error>'
  end else
  begin
    errmsg := FindAndReplace(errmsg, '<error>', '');
    errmsg := FindAndReplace(errmsg, '</error>', '');
    errmsg := FindAndReplace(errmsg, '"', '^^dq');
    result := '{"error":[{"msg":"' + errmsg +'"}]}';
  end;
end;

function TASBTStructObj.CreateResultWithAppSessionKey(xmldoc : ixmldocument ; jsonresult : widestring) : WideString;
begin
  if xmldoc <> nil then
  begin
    xmldoc.DocumentElement.Attributes['appsessionkey'] := appsessionkey;
    result := trim(xmldoc.XML.Text);
  end else
  begin
    if jsonresult <> '' then result := '{"appsessionkey":[{"value":"' + appsessionkey +'"}]}' + '#$#' + jsonresult
    else result := '{"appsessionkey":[{"value":"' + appsessionkey +'"}]}' ;
  end;
end;

Function TASBTStructObj.FindAndReplace(S:String;FindWhat:String;ReplaceWith: String):String;
var p:Integer;
begin
     Result:=s;
     p:=Pos(FindWhat,S);
     if p=0 then exit;
     Result:='';
     while p>0 do
     begin
          Result:=Result + copy(s,1,p-1) + ReplaceWith;
          Delete(s,1,p+length(FindWhat)-1);
          p:=Pos(FindWhat,S);
     end;
     Result := Result + s;
end;

function TASBTStructObj.GetPopGridDcs(flist : TStringList) : String;
  var i : integer;
      s : String;
begin
  result := '';
  for i := 0 to flist.Count - 1 do
  begin
    s := flist.Strings[i];
    if pos('$',s) > 0 then
    begin
      delete(s,pos('$',s)+1,5);
      result := result + quotedstr(s) + ',';
    end;
  end;
end;

function TASBTStructObj.FindParentField(fldName : String; fno : integer) : String;
  var pidx : integer;
      sb : String;
begin
  if dbcall.struct.popgrids.Count > 0 then
  begin
    for pidx := 0 to dbcall.struct.popgrids.Count - 1 do
    begin
       if pPopGrid(dbcall.struct.popgrids[pidx]).Parent = 'dc'+inttostr(fno) then
       begin
         sb := pPopGrid(dbcall.struct.popgrids[pidx]).ParentField + ',';
         result := dbm.gf.GetNthString(sb,1);
         break;
       end;
    end;
  end;
end;

function TASBTStructObj.RefreshGridDependents(xml,sxml:string):string;
var dcname,x,s,formload_fldlist:String;
    k,fno : integer;
begin
  result := '';

  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
    if formload_fldlist <> '' then
    begin
      result := RefreshGridDependentsNew(xml,sxml,formload_fldlist) ;
      exit;
    end;
  end;

  servicename:='Refresh Grid Dependents';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in RefreshGridDependents') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice RefreshGridDependents');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice RefreshGridDependents');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice RefreshGridDependents');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing RefreshGridDependents webservice');
    dbm.gf.dodebug.msg('---------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcnames']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcnames']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      dcname := xmldoc.DocumentElement.Attributes['dcname'];
      ASBDataObj.CreateAndSetDbCall(x,sxml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.SubmitClientValuesToSD;
      dcname := dcname + ',';
      k := 1;
      while true do
      begin
        s := dbm.gf.GetnthString(dcname,k);
        if s = '' then break;
        inc(k);
        delete(s,1,2);
        if s <> '' then fno := strtoint(s);
        dbCall.RefreshGridDependents(dbcall.Struct.GetFrame(fno));
        {  as per the discussion with sab, auto processing of gridtogrid dep. stopped. Will be handling at client side.
        RefreshDependentFGs;
        GridDependentOtherGridFieldsToJSON(dbcall.GridDependentOtherGridFields);
        }
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
      end;
      AsbDataObj.EndDataJSON;
      Result:=AsbDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing RefreshGridDependents webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.RefreshGridDependentsNew(xml,sxml,formload_fldlist:string):string;
var dcname,x,s,formload_fldlistdc,loadflag,fldlist,fglist,GridDependentFields:String;
    k,fno,i,j,r : integer;
    fm : pFrm;
    fd,dfd : pFld;
begin
  result := '';
  servicename:='Quick Refresh Grid Dependents';
  try
    dbm := nil;
    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in RefreshGridDependents') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice RefreshGridDependents');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice RefreshGridDependents');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice RefreshGridDependents');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick RefreshGridDependents webservice');
    dbm.gf.dodebug.msg('------------------------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcnames']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcnames']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];
      dcname := xmldoc.DocumentElement.Attributes['dcname'];

      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,visibleDCs,dcname+'~'+dcname,dcname,StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.depCall := true;
      ASBDataObj.SubmitClientValuesToSD_New;
      dbcall.struct.MakeProperStartIndex;
      dbCall.StoreData.depCall := ASBDataObj.depCall;
      delete(dcname,1,2);
      if dcname <> '' then fno := strtoint(dcname);
      fm:=dbcall.Struct.GetFrame(fno);
      dbm.gf.dodebug.Msg('Grid Dependends Refresh');
      DbCall.RefreshGridDependents(fm);
      AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
      AsbDataObj.EndDataJSON;
      Result:=AsbDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  dbm.gf.dodebug.msg('Executing RefreshGridDependents webservice over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

Procedure TASBTStructObj.GetDependents(FieldName:String; RowNo:integer);
var fd, dfd:pFld;
    fm, dfm:pFrm;
    frec:pFieldRec;
    fg:pFg;
    i, j, actrow:integer;
    DCInJSON, DCNum, SearchResult, NewValue, S , v : String;
    IdValue : Extended;

    procedure DCTOJSON(jfd:pFld);
    var rcount:integer;
    begin
      if pos(DCNum, DCInJSON)>0 then exit;
      RCount := dbcall.StoreData.RowCount(jfd.FrameNo);
      if actrow > -1 then
         asbDataObj.DCToJSON(jfd.FrameNo,RCount,actrow)
      else asbDataObj.DCToJSON(jfd.FrameNo,RCount);
      DCInJSON:=DCInJSON+','+inttostr(jfd.FrameNo)+',';
    end;
begin
  dbm.gf.DoDebug.msg('Refreshing Dependents');
  fd:=DbCall.Struct.GetField(FieldName);
  if not assigned(fd)  then exit;
  if not assigned(fd.Dependents) then exit;

  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  if fm.PopIndex > -1 then fm.HasDataRows := true;  // This is send dummy row to client side id getdependency call comes from popgrid field.

  DCInJSON:='';
  asbDataObj.jsonstr:='';
  SearchResult:='';
  actrow := -1;

  if (fd.txtSelection) then begin
    NewValue:=dbcall.StoreData.GetFieldValue(fd.FieldName, RowNo);
    if NewValue<>'' then begin
      //for pagination--
      dbm.gf.pagination_pageno := 1;
      if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
         dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
      if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
         dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
      //--
      if not dbcall.Validate.ValidatePickList(fd, NewValue, rowno) then begin
        dbcall.StoreData.SubmitValue(fd.FieldName, rowno, '', '', 0, 0, 0);
        dbcall.Parser.RegisterVar(fd.FieldName,fd.DataType[1],'');
        asbDataObj.act.createmsgnode(dbcall.Validate.ErrorStr);
        SearchResult := MakeSearchResultForGetDep(fd,'',NewValue,'no',fd.QSelect);
        if SearchResult = '{"pickdata":[{"rcount":"0"},{"fname":""}]}' then SearchResult:= '';
      end else
      begin
        dbm.gf.pagination_totalrows := 1;
        SearchResult := MakeResultForGetDep(fd,SearchResult,NewValue,'yes',fd.QSelect);
        if SearchResult = '{"pickdata":[{"rcount":"0"},{"fname":""}]}' then SearchResult:= '';
        if assigned(fd.QSelect) then
        begin
          if fd.QSelect.CDS.Active then
          begin
            IdValue := 0;
            if fd.SourceKey then
            begin
              IdValue:=fd.QSelect.CDS.Fields[0].AsFloat;
              NewValue:=fd.QSelect.CDS.Fields[1].AsString
            end else NewValue:=fd.QSelect.CDS.Fields[0].AsString;
            dbcall.storedata.submitvalue(fd.FieldName, rowno, NewValue, '', IdValue, 0, 0);
            dbcall.Parser.RegisterVar(fd.FieldName,fd.DataType[1],NewValue);
          end;
        end;
      end;
      dbm.gf.pagination_pageno := 0;
    end;
  end;

  if fd.asgrid then begin
    dbcall.Validate.RegRow(fd.FrameNo, RowNo);
    actrow := RowNo;
    DCTOJSON(fd);
    actrow := -1;
    if (fd.PopIndex>-1) and (fd.IsParentField) and (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
      dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
    RefreshRow(fd, RowNo);
    dbCall.RefreshGridDependents(fm);
//    GridDependentOtherGridFieldsToJSON(dbcall.GridDependentOtherGridFields);  as per the discussion with sab, auto processing of gridtogrid dep. stopped. Will be handling at client side.
    AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
  end else begin

    for I := 0 to fd.Dependents.count-1 do begin

      dbm.gf.DoDebug.msg('Refreshing '+fd.Dependents[i]+ ' Type '+fd.DependentTypes[i+1]);
      if fd.DependentTypes[i+1]='d' then begin
        DCTOJSON(fd);
        s:=fd.Dependents[i];
        if lowercase(copy(s,1,2)) = 'dc' then delete(s,1,2);
        DCNum:=','+s+',';
        if (pos(dcnum, DCInJSON)>0) then continue;
        if (fd.PopIndex>-1) and (fd.IsParentField) and (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
           dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
        DCInJSON:=DCInJSON+DoPopup(StrToInt(s), RowNo);
        continue;
      end else if fd.DependentTypes[i+1]='g' then begin
        DCTOJSON(fd);
        s:=fd.Dependents[i];
        delete(s,1,2);
        DCInJSON:=DCInJSON+DepFillGrid(StrToInt(s));
        continue;
      end;

      dfd:=dbcall.struct.GetField(fd.dependents[i]);
      if not assigned(dfd) then continue;
      dfm:=dbcall.Struct.GetFrame(dfd.FrameNo);
      DCNum:=','+inttostr(dfd.FrameNo)+',';
      if (dfd.AsGrid) and (pos(dcnum, DCInJSON)>0) then continue;
      if (not fd.AsGrid) and (not dfd.AsGrid) then begin
        if dfd.DataType = 'i' then begin
          if dfd.Tablename <> '' then
            asbDataObj.ImageFieldToJSON(dfd)
          else begin
            If (dfd.Exprn >= 0) Then Begin
              If Dbcall.Parser.EvalPrepared(dfd.Exprn) Then
              begin
                v := Dbcall.Parser.Value;
                if v <> '' then
                  asbDataObj.ImageFieldToJSON(dfd,v)
              end;
            end;
          end;
          continue;
        end;
        if (dfd.ModeofEntry = 'autogenerate') then begin
          if DbCall.StoreData.LastSavedRecordId = 0 then
            DbCall.Validate.RefreshField(dfd, rowno,true)
        end else
          DbCall.Validate.RefreshField(dfd, rowno,true);
        DCToJSON(dfd);
        ASBDataObj.FieldToJSON(dfd, dbcall.storedata.GetFieldRec(dfd.FieldName, RowNo));
      end else if (fd.AsGrid) and (not dfd.AsGrid) then begin
        if (dfd.ModeofEntry = 'autogenerate') then begin
          if DbCall.StoreData.LastSavedRecordId = 0 then
            DbCall.Validate.RefreshField(dfd, 1,true);
        end else
          DbCall.Validate.RefreshField(dfd, 1,true);
        DCToJSON(dfd);
        ASBDataObj.FieldToJSON(dfd, dbcall.storedata.GetFieldRec(dfd, 1));
      end else if (not fd.AsGrid) and (dfd.AsGrid) then begin
//        DCToJSON(dfd);
        DCInJSON:=DCInJSON+','+inttostr(dfd.FrameNo)+','+NonGridToGrid(fd, dfd);;
      end else if (fm.popindex>-1) and (dfd.AsGrid) then begin
        frec:=dbcall.storedata.getfieldrec(fd, RowNo);
        DbCall.Validate.RefreshField(dfd, frec.ParentRowNo);
        DCTOJSON(dfd);
        ASBDataObj.FieldToJSON(dfd, dbcall.storedata.GetFieldRec(dfd, frec.ParentRowNo));
      end;
    end;
  end;
  //--Below code deals with fillgrid defined for fields which are not directly dependant for given field--//
//  DCInJSON:=DCInJSON+RefreshDependentFGs;  as per the discussion with sab, auto processing of gridtogrid dep. stopped. Will be handling at client side.
  //--//
  asbDataObj.EndDataJSON;
  if SearchResult<>'' then
    asbDataObj.JSONStr := asbDataObj.JSONStr + '*$*' + SearchResult;
end;

function TASBTStructObj.RefreshDependentFGs : String ;
  var i : integer;
  s : string;
begin
  result := '';
  //--Below code deals with fillgrid defined for fields which are not directly dependant for given field--//
  i:=1;
  delete(dbcall.dependentfgs,1,1);
  while true do begin
    s:=dbm.gf.getnthstring(dbcall.dependentfgs,i);
    if s='' then break;
    delete(s,1,2);
    result:=result+IndirectDepFillGrid(StrToInt(s));
    inc(i);
  end;
  //--//
end;

procedure TASBTStructObj.RefreshRow(fd:pFld; RowNo:Integer);
var r, j, rcount, PopIndex:integer;
    fm:pFrm;
    fld:pfld;
    frec:pFieldRec;
    ov : string;
begin
  dbm.gf.DoDebug.msg('Refreshing row '+IntToStr(RowNo)+' in frame '+IntToStr(fd.frameno));
  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
    fld:=pFld(dbcall.struct.flds[j]);
    PopIndex:=PopAtField(fld);
    frec:=dbcall.storedata.GetFieldRec(fld.FieldName, rowno);
    if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
      RefreshPopUp(PopIndex, fd, rowno);
    if (fd.Dependents.IndexOf(fld.FieldName)>-1) and (dbcall.Validate.FieldParentsBound(fld, rowno)) then begin
      if assigned(frec) then ov := frec.Value;
      dbcall.validate.RefreshField(fld, rowno,true);
      if not assigned(frec) then
        frec:=dbcall.storedata.GetFieldRec(fld.FieldName, rowno);
      ASBDataObj.FieldToJSON(fld, frec);
    end else if assigned(frec) then
      dbcall.Parser.Registervar(fld.FieldName, fld.DataType[1], frec.value);
   if assigned(frec) and (fld.PopIndex > -1) and (fld.IsParentField) and (frec.Value = '')  then
      dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
   if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then begin
     dbcall.validate.UpdatePopParentChanges(fld,frec.Value,ov,rowno);
     RefreshPopUp(PopIndex, fd, rowno);
   end;
  end;
end;

function TASBTStructObj.NonGridToGrid(fd,dfd:pFld) : string;
var r, j, rcount, PopIndex:integer;
    fm:pFrm;
    fld:pfld;
    frec:pFieldRec;
    v : String;
begin
  result := '';
  dbm.gf.DoDebug.msg('Refreshing dependents of '+fd.FieldName+' in grid '+IntToStr(dfd.FrameNo));
  fm:=dbcall.Struct.GetFrame(dfd.FrameNo);
  rcount:=dbcall.StoreData.RowCount(dfd.FrameNo);
  if not fm.Popup then
  begin
    if rcount = 0 then
    begin
      rcount := 1;
      fm.HasDataRows := false;
      for r := 1 to rcount do begin
        dbcall.validate.RegRow(dfd.FrameNo, r);
        for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
          fld:=pFld(dbcall.struct.flds[j]);
          if fd.Dependents.IndexOf(fld.FieldName)>-1 then
          begin
            dbcall.validate.RefreshField(fld, r,true);
          end;
        end;
      end;
      fld := dbcall.struct.GetField('validrow'+inttostr(fm.FrameNo));
      if fld <> nil then
      begin
         v := dbcall.validate.RefreshField(fld, 1,true);
         if uppercase(v) = 'T' then fm.HasDataRows := true;
      end;
      for r := 1 to rcount do begin
        dbcall.validate.RegRow(dfd.FrameNo, r);
        for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
          fld:=pFld(dbcall.struct.flds[j]);
          PopIndex:=PopAtField(fld);
          frec:=dbcall.storedata.GetFieldRec(fld.FieldName, r);
          if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
            result := result + RefreshPopUp(PopIndex, fd, r);
          if fd.Dependents.IndexOf(fld.FieldName)>-1 then begin
            if not assigned(frec) then
              frec:=dbcall.storedata.GetFieldRec(fld.fieldname, r);
            ASBDataObj.FieldToJSON(fld, frec);
          end else if assigned(frec) then
            dbcall.Parser.Registervar(fld.FieldName, fld.DataType[1], frec.value);
          if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then
            result := result + RefreshPopUp(PopIndex, fd, r);
        end;
      end;
      exit;
    end;
  end;
  for r := 1 to rcount do begin
    dbcall.validate.RegRow(dfd.FrameNo, r);
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fld:=pFld(dbcall.struct.flds[j]);
      PopIndex:=PopAtField(fld);
      frec:=dbcall.storedata.GetFieldRec(fld.FieldName, r);
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
        result := result + RefreshPopUp(PopIndex, fd, r);
      if fd.Dependents.IndexOf(fld.FieldName)>-1 then begin
        dbcall.validate.RefreshField(fld, r,true);
        if not assigned(frec) then
          frec:=dbcall.storedata.GetFieldRec(fld.fieldname, r);
        ASBDataObj.FieldToJSON(fld, frec);
      end else if assigned(frec) then
        dbcall.Parser.Registervar(fld.FieldName, fld.DataType[1], frec.value);
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then
        result := result + RefreshPopUp(PopIndex, fd, r);
    end;
  end;
end;

function TASBTStructObj.PopAtField(dfd:pFld):integer;
var i:integer;
begin
  result:=-1;
  for I := 0 to dbcall.struct.popgrids.count-1 do begin
    if (pPopGrid(dbcall.struct.popgrids[i]).ParentFrameNo=dfd.FrameNo) and (pPopGrid(dbcall.struct.popgrids[i]).PopAt=dfd.OrderNo) then begin
      result:=i;
      break;
    end;
  end;
end;


function TASBTStructObj.RefreshPopup(PopIndex:integer; fd:pFld; RowNo:integer) : string ;
Var ParentList, ActualRows : TStringList;
    fm:pFrm;
    popgrid : pPopGrid;
    frec:pFieldRec;
    i, r, j:integer;
    fld:pFld;
    HasDependents:Boolean;
    s : String;
begin
  result := '';
  if popindex < 0 then exit;
  PopGrid:=pPopGrid(dbcall.struct.PopGrids[PopIndex]);
  HasDependents:=false;
  fm:=dbcall.Struct.GetFrame(popgrid.FrameNo);
  //If fd has 'd' dependent for the given popgrid.
  //if true then call do popup else
  for i := 0 to fd.Dependents.count-1 do begin
    if fd.DependentTypes[i+1]='d' then begin
      dbm.gf.DoDebug.msg('Refreshing '+fd.Dependents[i]+ ' Type '+fd.DependentTypes[i+1]);
      s:=fd.Dependents[i];
      if lowercase(copy(s,1,2)) = 'dc' then delete(s,1,2);
      result := result + DoPopup(fm.FrameNo, RowNo);
      exit;
    end;
  end;
  for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
    fld:=pFld(dbcall.struct.flds[j]);
    if fd.Dependents.IndexOf(pFld(dbcall.struct.flds[j]).FieldName)>-1 then begin
      HasDependents:=true;
      break;
    end;
  end;
  if not HasDependents then exit;
  dbm.gf.DoDebug.msg('Refreshing popup fields '+IntToStr(popgrid.frameno));

  //Get row numbers of sub grid rows that are for this parent row.
  ParentList:=TStringList.create;
  ActualRows:=TStringList.create;
  dbcall.Validate.GetParentValue(popgrid.FrameNo,RowNo,popgrid.ParentField,ParentList);
  if popgrid.AutoFillFld then
    ParentList.Add('sub'+Trim(inttostr(popgrid.FrameNo))+'_autofill=T');
  ActualRows.CommaText := dbcall.validate.GetActualRows(popgrid.FrameNo,ParentList);

  //Refresh dependents in all rows in pop grid that are related to this parent row.
  for i := 0 to ActualRows.Count-1 do begin
    r:=StrToInt(ActualRows[i]);
    dbcall.validate.RegRow(fld.FrameNo, r);
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fld:=pFld(dbcall.struct.flds[j]);
      frec:=dbcall.storedata.GetFieldRec(fld.FieldName, r);
      if fd.Dependents.IndexOf(fld.FieldName)>-1 then begin
        dbcall.validate.RefreshField(fld, r);
        if not assigned(frec) then
          frec:=dbcall.storedata.GetFieldRec(fld.fieldname, r);
        ASBDataObj.FieldToJSON(fld, frec);
      end else if assigned(frec) then
        dbcall.Parser.RegisterVar(fld.FieldName, fld.DataType[1], frec.Value);
    end;
  end;
  ParentList.free;
  ActualRows.Free;
end;

function TASBTStructObj.DoPopup(FrameNo, RowNo:Integer):String;
var Refreshed:Boolean;
    popgrid:pPopGrid;
    i, pidx, rcount:integer;
begin
  result:='';
  popgrid:=nil;
  for i := 0 to dbcall.struct.popgrids.count-1 do begin
    if pPopGrid(dbcall.struct.popgrids[i]).FrameNo=FrameNo then begin
      popgrid:=pPopGrid(dbcall.struct.popgrids[i]);
      break;
    end;
  end;
  if not assigned(popgrid) then exit;
  dbm.gf.DoDebug.msg('Refreshing popup '+IntToStr(popgrid.FrameNo)+' Parent row '+IntToStr(RowNo));
  if DbCall.DoPopup(FrameNo, RowNo) then begin
    rcount:=dbcall.storedata.RowCount(FrameNo);
    asbDataObj.PopDCToJSON(FrameNo, RCount);
    result:=result+','+inttostr(FrameNo)+',';
    AsbDataObj.PopGridToJSON(popgrid, Rowno, true);
  end;
end;

function TASBTStructObj.DepFillGrid(FrameNo:Integer):String;
var i, RCount, fno:integer;
    fg:pFg;
    fm:pFrm;
begin
  dbm.gf.DoDebug.msg('Filling dependent grid '+ IntToStr(FrameNo));
  result:='';
  fg:=ExecFillGrid(FrameNo, fgFromDependents,'');
  if assigned(fg) then begin
    fm:=dbcall.Struct.GetFrame(FrameNo);
    result:=','+inttostr(FrameNo)+',';
    RCount := dbcall.StoreData.RowCount(FrameNo);
    if RCount = 0 then fm.HasDataRows := false;
    AsbDataObj.GridToJSON(fg.TargetFrame, true);
    if fm.jsonstr = '' then AsbDataObj.DummyDCNodeToJSON(FrameNo,1,'d*,i1');  // as per the requirement for .NET
    AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
    PopGridJSON(FrameNo,RCount);
    for i := 0 to dbcall.struct.popgrids.Count - 1 do begin
      if FrameNo=pPopGrid(dbcall.struct.popgrids[i]).ParentFrameNo then begin
        fno:=pPopGrid(dbcall.struct.popgrids[i]).FrameNo;
        result:=result+','+inttostr(fno)+',';
      end;
    end;
  end;
end;

function TASBTStructObj.IndirectDepFillGrid(FrameNo:Integer):String;
var i, RCount, fno:integer;
    fg:pFg;
    fm : pFrm;
begin
  dbm.gf.DoDebug.msg('Filling dependent grid '+ IntToStr(FrameNo));
  result:='';
  fg:=ExecFillGrid(FrameNo, fgFromDependents,'');
  if assigned(fg) then begin
    fm:=dbcall.Struct.GetFrame(FrameNo);
    fm.jsonstr := '' ;
    result:=','+inttostr(FrameNo)+',';
    RCount := dbcall.StoreData.RowCount(FrameNo);
    AsbDataObj.GridToJSON(fg.TargetFrame, true);
    AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
    PopGridJSON(FrameNo,RCount);
    for i := 0 to dbcall.struct.popgrids.Count - 1 do begin
      if FrameNo=pPopGrid(dbcall.struct.popgrids[i]).ParentFrameNo then begin
        fno:=pPopGrid(dbcall.struct.popgrids[i]).FrameNo;
        result:=result+','+inttostr(fno)+',';
      end;
    end;
  end;
end;

procedure TASBTStructObj.AddGridRow(FrameNo, RowNo:Integer);
var fm:pFrm;
    i:integer;
    frec:pFieldRec;
    fld:pFld;
begin
   dbCall.Parser.RegisterVar('ActiveRow', 'n', inttostr(rowno));
   fm:=dbcall.Struct.GetFrame(FrameNo);
   For i:=fm.startindex to fm.startindex+fm.FieldCount-1 do begin
      fld:=pFld(dbcall.Struct.flds[i]);
      Frec:=dbcall.Validate.PrepareField(fld, RowNo);
      if assigned(frec) then
        asbDataObj.FieldToJSON(fld, frec);
   End;
  DbCall.RefreshGridDependents(fm);
  //AsbDataObj.GridDependentsToJSON(dbcall.Refreshed)
  asbDataObj.EndDataJSON;
end;

procedure TASBTStructObj.CreateRecIdDataNode;
  var j : integer;
begin
  ASBDataObj.jsonstr := '';
  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     ASBDataObj.DCToJSON(pFrm(dbcall.StoreData.structdef.frames[j]),-1);
  end;
  ASBDataObj.EndDataJSON;
end;

function TASBTStructObj.GetRecordIDtoLoad(n : ixmlnode) : extended;
  var i : integer;
     n1,n2 : ixmlnode;
     f,v,d,t,s : string;
     fld : pFld;
begin
  f := '';v:='';d:='';s:='';
  for i := 0 to n.ChildNodes.Count - 1 do
  begin
      n1 := n.ChildNodes[i];
      if n1.IsTextElement then
      begin
         fld := dbCall.struct.GetField(vartostr(n1.NodeName));
         if assigned(fld) and (fld.SaveValue) then
         begin
            f := f + vartostr(n1.NodeName) + ',';
            if fld.SourceKey then
            begin
               s := vartostr(n1.NodeName) + '_id';
               n2 := n.ChildNodes.FindNode(s);
               if assigned(n2) and n2.IsTextElement then v := vartostr(n2.NodeValue) + '~';
               d := fld.DataType ;
               break;
            end else
            begin
              t := fld.DataType ;
              d := d + t;
              v := v + vartostr(n1.NodeValue) + '~';
            end;
         end else
         begin
            f := f + vartostr(n1.NodeName) + ',';
            fld := dbCall.struct.GetField(vartostr(n1.NodeName));
            if assigned(fld) then t := fld.DataType
            else t := 'c';
            d := d + t;
            if t = 'c' then v := v + vartostr(n1.NodeValue) + '~'
            else  v := v + vartostr(n1.NodeValue) + '~';
         end;
      end;
  end;
  dbm.gf.DoDebug.msg('Getting recordid to load data in doformload with parameters : ' + f + ' with values ' + v);
  if f <> '' then result := dbcall.GetRecordId(f,v,d);
  dbm.gf.DoDebug.msg('Recordid to load : ' + floattostr(result));
end;

Procedure TASBTStructObj.GridDependentOtherGridFieldsToJSON(GridDependentOtherGridFields:String);
var i:integer;
  s,sfld,s1,j,s2:string;
  fld:pFld;
begin
  i:=1;
  sfld := ',';
  s1 := GridDependentOtherGridFields;
  delete(GridDependentOtherGridFields,1,1);
  delete(GridDependentOtherGridFields, length(GridDependentOtherGridFields), 1);
  while True do begin
    s:=dbcall.dbm.gf.getnthstring(GridDependentOtherGridFields, i);
    inc(i);
    if s='' then break;
    fld:=dbcall.struct.getfield(s);
    if not assigned(Fld) then continue;
    j := inttostr(fld.FrameNo);
    s2 := ',dc'+j+',';
    if pos(s2,dbcall.DependentFgs) > 0 then continue;
    if pos(','+j+',',sfld) <= 0 then
    begin
       GridDependentOtherGridToJSON(fld.frameno,s1);
       sfld := sfld + j + ',';
    end;
  end;
end;

procedure TASBTStructObj.GridDependentOtherGridToJSON(FrameNo:integer; GridDependentOtherGridFields:String);
var j, PopIndex, rcount, r:integer;
    fm:pFrm;
    fd:pFld;
    frec:pFieldRec;
begin
  dbm.gf.dodebug.msg('Other Grid Dependents To JSON '+Inttostr(FrameNo));
  fm:=dbcall.Struct.GetFrame(FrameNo);
  rcount:=dbcall.StoreData.RowCount(fm.FrameNo);
  if rcount=0 then rcount:=1;
  for r := 1 to rcount do begin
    dbcall.validate.RegRow(FrameNo, r);
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fd:=pFld(dbcall.struct.flds[j]);
      if pos(','+fd.FieldName+',',GridDependentOtherGridFields) <= 0 then continue;
      PopIndex:=PopAtField(fd);
      frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
        RefreshPopUp(PopIndex, fd, r);
      if (dbcall.Validate.FieldParentsBound(fd, r)) then begin
        dbcall.validate.RefreshField(fd, r);
        if not assigned(frec) then
          frec:=dbcall.storedata.GetFieldRec(fd.FieldName, r);
        ASBDataObj.FieldToJSON(fd, frec);
      end else if assigned(frec) then
        dbcall.Parser.Registervar(fd.FieldName, fd.DataType[1], frec.value);
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then
        RefreshPopUp(PopIndex, fd, r);
      end;
  end;
  if rcount=1 then ASBDataObj.ChangeHasDataRowStatus(fm);
end;

Procedure TASBTStructObj.CopyTransAndSave(SrcTransid:String; SrcRecid:Extended; FldDets:String);
var srcDbCall, tgtDbCall : TDbcall;
    i, rno, cpos : integer;
    trec : pFieldRec;
    FldDet, FldName, FldVal, s : String;
    flag : Boolean;
begin
  try
    try
      flag := True;
      srcDbCall := TDbcall.create;
      srcDbcall.dbm := axprovider.dbm;
      srcDbcall.axprovider:=axprovider;
      srcDbcall.transid := SrcTransid;
      srcDbCall.LoadData(SrcRecid);
      tgtDbcall := TDbCall.create;
      tgtDbcall.dbm := axprovider.dbm;
      tgtDbcall.axprovider:=axprovider;
      tgtDbCall.transid := SrcTransid;
      for i := 0 to srcDbCall.StoreData.FieldList.Count - 1 do begin
        trec := pFieldRec(srcDbCall.StoreData.FieldList[i]);
        tgtDbcall.StoreData.SubmitValue(trec.FieldName,trec.RowNo,trec.Value,'',trec.IdValue,0,0);
        tgtDbcall.parser.RegisterVar(trec.fieldname, trec.DataType[1], trec.Value);
      end;
      i := 1;
      FldDet := dbm.gf.GetNthString(FldDets,i,'~');
      while FldDet <> '' do begin
        cpos := Pos('=',FldDet);
        if cpos > 0 then begin
          FldName := Trim(Copy(FldDet,1,cpos-1));
          FldDet := Copy(FldDet,cpos+1,Length(FldDet));
          s := Trim(dbm.gf.GetNthString(FldDet,1));
          if s = '' then
            rno := 1
          else
            rno := strtoint(s);
          FldVal := Trim(dbm.gf.GetNthString(FldDet,2));
          if Copy(FldVal,1,1) = ':' then begin
            FldVal := Copy(FldVal,2,Length(FldVal));
            FldVal := DbCall.Parser.GetVarValue(FldVal);
          end;
          tgtDbcall.StoreData.SubmitValue(FldName,rno,FldVal,'',0,0,0);
        end;
        inc(i);
        FldDet := dbm.gf.GetNthString(FldDets,i,'~');
      end;
      tgtDbCall.Validate.Loading := False;
      flag := dbm.InTransaction;
      if not flag then dbm.StartTransaction(AxProvider.dbm.gf.connectionname);
      s := tgtDbcall.ValidateAndSave;
      if s<> '' then
      begin
        dbm.gf.ErrorInActionExecution := s;
        Raise Exception.Create(s);
      end;
      if not flag then dbm.Commit(AxProvider.dbm.gf.connectionname);
    except
      On E:Exception do begin
        if assigned(dbm.Connection) then
        begin
          if not flag then dbm.RollBack(AxProvider.dbm.gf.connectionname);
        end;
        dbm.gf.DoDebug.msg('Error in CopyTransAndSave procedure. '+E.Message);
        Raise Exception.Create(E.Message);
      end;
    end;
  finally
    if assigned(srcDbcall) then begin
      srcDbCall.Destroy;
      srcDbcall := nil;
    end;
    if assigned(tgtDbCall) then begin
      tgtDbCall.Destroy;
      tgtDbcall := nil;
    end;
  end;

end;



Function TASBTStructObj.SendMail(XmlString:IXMlNode):String;
var
  TempParser : TProfitEval;
begin
  Result := '';
  TempParser := TProfitEval.Create(AxProvider);
  TempParser.Assign(ASBDataObj.act.CoreParser);
  ASBDataObj.act.CoreParser.Assign(dbCall.Parser);
  Result := ASBDataObj.act.SendMail(XmlString);
  ASBDataObj.act.CoreParser.Assign(TempParser);
  TempParser.Destroy;
  TempParser := nil;
end;

function TASBTStructObj.ChangeFormatForSplitresult(str,Param:string):String;//If splitresultforclient is false
var docid,enqno:string;
begin
  if Param = '1' then
  begin
    docid := copy(str,1,(pos('-',str)-1));
    enqno := copy(Str,(pos('-',str)+1),length(str));
    str :='","'+docid+'":"'+enqno+'"';
    Result := Str;
  end else if Param ='2'  then
  begin
    Delete(Str,Pos('[',str),1);
    Delete(Str,Pos(']',str),2);
    Result := Str+',';
  end else if Param='3' then
  begin
    str := FindAndReplace(str, '"', '\"');
    Result := Str;
  end
  else if param='4' then
  begin
    Delete(Str,(length(Str)-7),length(Str));
    Delete(Str,1,7);
    Result := Str;
  end;
end;

{
On Form Load
1. Loop through sdef.flds
   a. Accept with Expression/Calculate fields
        a1. Evaluate expression if there are no parents
        a2. Evaluate field expression if all parents are bound
   b. Accept SQL
        b1. Get SQL result and bind if no parents.
        b2. Get SQL result & bind if all parents are bound
   c. Select SQL
        c1. Get SQL result and bind if no parents.
        c2.Get SQL result & bind if all parents are bound
        c3. If expression provided, set result to value.
   d. For grid fields do it only for one row. Ignore fields having grid dependency.
   e. Autogen fields - genereate next value.
   f. Fill grid
      f1. If no parameters, execute fill grid
      f2. If all parameters bound, execute fill grid.
   g. Pop grid with auto fill & all parent fields are bound
      g1. If no parameters, execute & fill pop grid.
      g2. If all parameters bound, execute & fill pop grid.

Fill grid
1. Fire SQL and get result.
2. for every row in SQL result
     2a. For every column in grid DC
        2aa. Enter Field for column
        2ab. If column is mapped, assign value to column
        2ac. Exit field
        2ad. If field is button field for a pop grid, call fill popup

Update fill grid dependents
1. GridDependents = List of depenedent fields of grid fields that are not in grid.
2. Loop through GridDependents
   RefreshField

GetDependents
1. FieldDependents = List of all depdendent fields for a given field
2. Sort the FieldDependents on FrameNo, ParentOrderNo, OrderNo.
3. Loop through FieldDependents
   3a. If NG to NG, refresh field
   3b. If G to G, refresh field
   3c. If NG to G, refresh field in all rows.
   3d. If G to PG, refresh field in all sub rows for this parent row.
   3e. If PG to G, refresh field in Parent row
   3f. If G to NG, refresh field.
   3g. If PG to NG, refresh field.

Load DC
1. If fill grid defined for DC and fill grid contains no parameters or only global vars as params
   Execute fill grid
2  Else if no fill grid
   2a. For every field in DC
      2aa. If field is select with no parameters or only global vars are params, open the query
      2ab. If field is grid field with no parameters or only global vars are params, open the query for every row.

Fill grid service
1. Call Fill grid
2. Call Fill grid dependents.

}



function TASBTStructObj.ExportData(S: String): WideString;
var x , Cond , SessionId , TransId , Qcsv , Ftype , Separator ,Filename ,
                         Header,Exp_Header,QueryStr ,SelectedFlds , withheader: String;
    rNode : ixmlnode;
    ViewFields, CondStr : TstringList;
    I,J: Integer;
    DataExp : TExportData;
    resultXml : IXMlDocument;
begin
  resultXml := LoadXMLData('<result> </result>');
  Result := '';
  servicename:='ExportData';
  if Assigned(ViewFields) then FreeAndNil(ViewFields);
  if Assigned(CondStr) then FreeAndNil(CondStr);

  ViewFields := TStringList.Create;
  CondStr := TStringList.Create;

  Exp_Header := '';
  Header := '';

  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in ExportData');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to ExportData Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to ExportData webservice');
    if vartype(xmldoc.DocumentElement.Attributes['type']) = varnull then   //export file type
      raise Exception.create('type tag not specified in call to ExportData webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if xmldoc.DocumentElement.HasAttribute('sessionid') then
       SessionId := vartostr(xmldoc.DocumentElement.Attributes['sessionid']);
    if xmldoc.DocumentElement.HasAttribute('filename') then
       Filename := vartostr(xmldoc.DocumentElement.Attributes['filename']);
    if xmldoc.DocumentElement.HasAttribute('type') then
       Ftype := vartostr(xmldoc.DocumentElement.Attributes['type']);
    if xmldoc.DocumentElement.HasAttribute('sep') then
       Separator := vartostr(xmldoc.DocumentElement.Attributes['sep']);
    if xmldoc.DocumentElement.HasAttribute('qcsv') then
       Qcsv := vartostr(xmldoc.DocumentElement.Attributes['qcsv']);
    if xmldoc.DocumentElement.HasAttribute('withheader') then
       withheader := vartostr(xmldoc.DocumentElement.Attributes['withheader']);


    SelectedFlds := vartostr(xmldoc.DocumentElement.ChildValues['map']);




    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := True;
    ConnectToProject(x);

    for I := 0 to Xmldoc.DocumentElement.ChildNodes.Count-1 do
    Begin
      if Xmldoc.DocumentElement.ChildNodes[i].Attributes['cat'] <> 'cond' then
      Begin
        for J := 0 to Xmldoc.DocumentElement.ChildNodes[i].ChildNodes.Count-1  do
        Begin
          Cond := (vartostr(Xmldoc.DocumentElement.ChildNodes[i].ChildValues['l'+ InttoStr(J+1)]));
          if Cond <> '' then
          begin
            AxProvider.dbm.gf.FindAndReplace(Cond,'&gt;','>');
            AxProvider.dbm.gf.FindAndReplace(Cond,'&lt;','<');
            AxProvider.dbm.gf.FindAndReplace(Cond,'&amp;','&');
            AxProvider.dbm.gf.FindAndReplace(Cond,'&apos;','''');
            AxProvider.dbm.gf.FindAndReplace(Cond,'&quot;','"');
            CondStr.Add(Cond);
          end;
        End;
      End;
    End;

    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing ExportData webservice');
    dbm.gf.dodebug.msg('-------------------------------');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       resultXml.DocumentElement.AddChild('experr').NodeValue := CreateErrNode(x,false)
    else
    begin
      x:= xmldoc.DocumentElement.Attributes['transid'];
      if Assigned(sdef) then sdef.Destroy;

      DataExp := TExportData.Create;
      DataExp.forExpHead := false;
      DataExp.Axp :=  AxProvider;
      DataExp.Qcsv := Qcsv;
      if Separator = '' then Separator := ',';
      DataExp.Separator := Separator;
      DataExp.WithHeader := Lowercase(WithHeader);
       if Assigned(sdef) then
      Begin
        sdef.Destroy;
        FreeAndNil(sdef);
      End;
      sdef := TStructDef.Create(AxProvider,x,'','');
      DataExp.sdef := sdef;
      if DataExp.WithHeader = 'true' then
      begin
        DataExp.forExpHead := true;
        DataExp.Exp_Header := DataExp.ListDcFields(SelectedFlds);
        DataExp.forExpHead := false;
      end;
      SelectedFlds := DataExp.ListDcFields(SelectedFlds);
      ViewFields.CommaText :=  SelectedFlds;

      if Not AnsiEndsStr('\',spath) Then Spath := spath +'\';
      DataExp.Expfile := spath+'axpert\'+dbm.gf.sessionid+'\'+Filename;   //+Ftype;
      DataExp.Transid := x;
      ForceDirectories(spath+'axpert\'+dbm.gf.sessionid);

      QueryStr := DataExp.CreateFrameSQl(sdef,ViewFields,CondStr);
      DataExp.sdef := sdef;
      resultXml.DocumentElement.AddChild('filename').NodeValue := Filename;
      resultXml.DocumentElement.AddChild('filepath').NodeValue := spath+'axpert\'+dbm.gf.sessionid+'\';
      resultXml.DocumentElement.AddChild('expcount').NodeValue := DataExp.ExecuteQuery(QueryStr,SelectedFlds,Ftype);
    End;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(dbm) then
        dbm.gf.dodebug.msg('Error : ' + e.Message);
      resultXml.DocumentElement.AddChild('experr').NodeValue := e.Message;
    end;
  end;
  Result := resultXml.DocumentElement.XML;
  if assigned(dbm) then begin
    dbm.gf.dodebug.msg('Executing ExportData webservice Over');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('');
    closeproject;
    if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(resultxml,'');
  end;
  if Assigned(ViewFields) then FreeAndNil(ViewFields);
  if Assigned(CondStr) then FreeAndNil(CondStr);
  if Assigned(xmlDoc) then xmlDoc := nil;
  if Assigned(sdef) then
  Begin
    sdef.Destroy;
  End;

end;




function TASBTStructObj.ImportData(S: String): WideString;
var x , Cond , SessionId , TransId , Qcsv , Ftype , Separator ,Filename , QueryStr ,SelectedFlds , MapIn_File : String;
    rNode : ixmlnode;
    I,J: Integer;
    DataImp : TImportData;
    ResulXml : IXMLDocument;
begin
  ResulXml := LoadXMLData('<result> </result>');
  result := '';
  servicename:='ImportData';

  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(s);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in ImportData');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to ImportData Webservice');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to ImportData webservice');
    if vartype(xmldoc.DocumentElement.Attributes['type']) = varnull then   //export file type
      raise Exception.create('type tag not specified in call to ImportData webservice');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if xmldoc.DocumentElement.HasAttribute('sessionid') then
       SessionId := vartostr(xmldoc.DocumentElement.Attributes['sessionid']);
    if xmldoc.DocumentElement.HasAttribute('filename') then
       Filename := vartostr(xmldoc.DocumentElement.Attributes['filename']);
    if xmldoc.DocumentElement.HasAttribute('type') then
       Ftype := vartostr(xmldoc.DocumentElement.Attributes['type']);
    if xmldoc.DocumentElement.HasAttribute('sep') then
       Separator := vartostr(xmldoc.DocumentElement.Attributes['sep']);
    if xmldoc.DocumentElement.HasAttribute('qcsv') then
       Qcsv := vartostr(xmldoc.DocumentElement.Attributes['qcsv']);
    if xmldoc.DocumentElement.HasAttribute('mapinfile') then
       MapIn_File := vartostr(xmldoc.DocumentElement.Attributes['mapinfile']);


    if (lowercase(Qcsv) = 'yes') and (Separator <> ',') then
       raise Exception.Create('If the file contains data within quotes means it should have comma(,) as separator.');
    SelectedFlds := vartostr(xmldoc.DocumentElement.ChildValues['map']);

    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing ImportData webservice');
    dbm.gf.dodebug.msg('-------------------------------');
    dbm.gf.dodebug.msg('Received XML : ' + s);
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
    begin
       ResulXml.DocumentElement.AddChild('imperr').NodeValue := CreateErrNode(x,false);
    end
    else
    begin
      x:= xmldoc.DocumentElement.Attributes['transid'];
      DataImp := TImportData.Create;
      DataImp.Axp :=  AxProvider;
      DataImp.transID :=  x;
      if Assigned(sdef) then
      Begin
        sdef.Destroy;
        FreeAndNil(sdef);
      End;
      sdef := TStructDef.Create(AxProvider,x,'','');
      DataImp.sdef := sdef;
      DataImp.Delimit := Separator;
      DataImp.Qcsv := Qcsv;
      DataImp.MapIn_File := MapIn_File;
      DataImp.Map_Flds := DataImp.ListDcFields(SelectedFlds);
      if Not AnsiEndsStr('\',spath) Then Spath := spath +'\';
      DataImp.Impfile := spath+'axpert\'+dbm.gf.sessionid+'\'+Filename;
      DataImp.ReportFile := spath+'axpert\'+dbm.gf.sessionid+'\'+x+'_result.txt';
      ForceDirectories(spath+dbm.gf.sessionid);
      ResulXml.DocumentElement.AddChild('filename').NodeValue := x+'_result.txt';
      ResulXml.DocumentElement.AddChild('filepath').NodeValue := spath+'axpert\'+dbm.gf.sessionid+'\';

      DataImp.ImportFile;
    End;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      ResulXml.DocumentElement.AddChild('imperr').NodeValue := e.Message;
    end;
  end;
  result := ResulXml.DocumentElement.XML;
  dbm.gf.dodebug.msg('Executing ImportData webservice Over');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  closeproject;
  if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(ResulXml,'');
  if Assigned(xmlDoc) then xmlDoc := nil;

end;

function TASBTStructObj.GetWFButtons(xml:string):string;
  var x,transid,recid : String;
begin
  result := '';
  servicename:='Get Workflow Buttons';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in CallWorkFlowAction');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openconnect:=True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetWFButtons webservice');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
     dbm.gf.dodebug.msg('');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else begin
      dbm.gf.dodebug.msg('Creating DbCall');
      transid:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recid:= vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      ASBDataObj.CreateAndSetDbCall(transid,'');
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.DbCall.act := ASBDataObj.act;
      dbcall.Parser.Registervar('recordid', 'n', recid);
      if dbcall.EnableApprovalBar(StrToFloat(recid)) then begin
        ASBDataObj.jsonstr := '$';   //as workflownode is getting added if jsonstr is not empty only
        ASBDataObj.CreateWorkflowNode;
        result :=   ASBDataObj.JsonStr;
      end else result := '';
    end
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg(E.Message);
      result := CreateErrNode(E.Message,false);
        if assigned(dbm.Connection) then
        begin
        x := dbm.GetAxpertMsg(e.Message);
        if x <> '' then
        begin
           result := CreateErrNode(x,false);
           dbm.gf.dodebug.msg('Error : ' + x);
        end;
        end;
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.StartWFAction(xml:string):string;
  var x,ptable,transid,recid,s : String;
begin
  result := '';
  servicename:='Workflow Action';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in CallWorkFlowAction');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openconnect:=True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing CallWorkFlowAction webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else begin
      dbm.gf.dodebug.msg('Creating DbCall');
      transid:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recid:= vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      ASBDataObj.CreateAndSetDbCall(transid,'');
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      ASBDataObj.DbCall.act := ASBDataObj.act;
      dbcall.Parser.Registervar('recordid', 'n', recid);
      ptable:=dbcall.struct.PrimaryTable;
      dbcall.CreateMapObjects;
      dbcall.Parser.OnLoadAndSave:=ASBDataObj.LoadAndSave;
      dbcall.Parser.OnLoadTrans := LoadTrans;
      dbcall.Parser.OnSaveTrans:=SaveTrans;
      dbcall.Parser.OnEndTrans := EndTrans;
      dbcall.SaveTrans := SaveTrans;
      dbcall.Parser.OnCopyTransAndSave := CopyTransAndSave;
      dbcall.LoadData(strtofloat(recid));
      s := SubmitAndValidateInWFAction(recid);
      if s <> '' then raise exception.Create(s);
      dbm.StartTransaction(connection.ConnectionName);
      dbcall.SaveData;
      dbm.Commit(connection.ConnectionName);
      dbm.gf.dodebug.msg('Transaction commited');
      { checked with Sab . This code need to relook }
      if (dbcall.CallWorkFlowApproval) and (dbcall.WorkFlow.ToBeSaved) then begin
        dbcall.StoreData.SetRecid(dbcall.LastSavedId);
        if (assigned(dbcall.MDMap)) and ((dbcall.MDMap.WorkFlow ='approve') or (dbcall.MDMap.WorkFlow='reject')) then
          dbcall.MDMap.SetInitOld := False
        else begin
          if dbcall.workflow.active then begin
            dbcall.MDMap.WorkFlow := '';
            dbcall.MDMap.SetInitOld := True;
          end;
        end;
        SaveTrans;
      end;
      result := 'done';
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg(E.Message);
      result := CreateErrNode(E.Message,false);
        if assigned(dbm.Connection) then
        begin
           if dbm.InTransaction then dbm.RollBack(connection.ConnectionName);
           x := dbm.GetAxpertMsg(e.Message);
        end;
        if x <> '' then
        begin
           result := CreateErrNode(x,false);
           dbm.gf.dodebug.msg('Error : ' + x);
        end;
    end;
  end;
  GenerateSessionAppKey := False;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false);
end;

function TASBTStructObj.UnlockTstructsRecord(xml: string): string;
 var x,transid,recid,suser : String;
begin
  result := '';
  servicename:='Unlock Tstructs Records';
  try
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in UnlockTstructsRecord');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in parameter');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise exception.create('recordid attribute not specified');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openconnect:=True;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing UnlockTstructsRecord webservice');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    dbm.gf.dodebug.msg('');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := x
    else begin
      transid:= vartostr(xmldoc.DocumentElement.Attributes['transid']);
      recid:= vartostr(xmldoc.DocumentElement.Attributes['recordid']);
      suser:= vartostr(xmldoc.DocumentElement.Attributes['unlockby']);
      axprovider.DeleteTransControl(recid,transid,suser);
      result := 'done';
    end
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg(E.Message);
      result := CreateErrNode(E.Message,false);
    end;
  end;
  GenerateSessionAppKey := False;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false);
end;

function TASBTStructObj.GetFillgridDefDetails(transid,fillgridname:string):string;
var dQry :TXds; s:String;
    i:integer;
begin
  dQry := dbm.GetXDS(nil);
  dQry.buffered := True;
  dQry.CDS.CommandText:='Select FormSize,Colsize from fillgriddef where transid=:t and fg_name=:fgname';
  dQry.AssignParam(0,transid,'c');
  dQry.AssignParam(1,fillgridname,'c');
  dQry.Open;
  if not dQry.CDS.IsEmpty then
    Result:=dQry.CDS.FieldByName('Formsize').AsString +'~'+dQry.CDS.FieldByName('Colsize').AsString
  else Result := '';
  dQry.Close;
  FreeAndNil(dQry);
end;


Function TASBTStructObj.AddToAppVars:String;
var i,ind : integer;
    fn, fv : String;
    vFld : pFld;
    gxds : TXDS;
begin
  gxds := nil;
  result := '';
  try
    for i := 0 to dbcall.struct.flds.Count-1 do
    begin
      vFld := dbcall.struct.flds[i];
      fn := vFld.FieldName;
      fv := dbcall.Parser.GetVarValue(vFld.FieldName);
      ind := dbm.gf.AppVars.IndexOfName(fn);
      if ind = -1 then
        dbm.gf.AppVars.Add(fn+'='+fv)
      else
        dbm.gf.AppVars.ValueFromIndex[ind] := fv
    end;
    gxds := dbm.GetXDS(nil);
    gxds.Edit('connections', 'sessionid = '+QuotedStr(dbm.gf.sessionid));
    gxds.submit('appvars', dbm.gf.appvars.Text, 'c');
    gxds.Post;
  except
    On E:Exception do
      Result := E.Message;
  end;
  if assigned(gxds) then
  begin
    gxds.close;
    gxds.Free;
  end;
end;

function TASBTStructObj.AutoGetSearchResultNew(s,sxml:string):string;
var x,sqlfld,v,SearchCond,sqltext:String;
    q,q1:TXDS;
    Validate : TValidate;
    n : ixmlnode;
begin
  result := '';
  servicename:='AutoGetSearchResult';
  SearchCond := '';
  try
    dbm := nil;
    dbcall := nil;
    q := nil;
    xmldoc := LoadXMLDataFromWS(s);
    StructXml := LoadXMLDataFromWS(sxml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in AutoGetSearchResult');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['sqlfield']) = varnull then
      raise Exception.create('field tag not specified in call to AutoGetSearchResult WebService');
    if vartype(xmldoc.DocumentElement.Attributes['value']) = varnull then
      raise Exception.create('value tag not specified in call to AutoGetSearchResult WebService');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if vartype(xmldoc.DocumentElement.Attributes['cond']) <> varnull then
      SearchCond := lowercase(trim(xmldoc.DocumentElement.Attributes['cond']));
    sqltext := vartostr(xmldoc.DocumentElement.ChildNodes.FindNode('sqltext').NodeValue);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ValidateSessionAppKey := False;
    SetDefaultValues := False;
    GetServerDT := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing AutoGetSearchResultNew webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
        if dbm.Connection.DbType = 'postgre' then
        begin
         if dbm.gf.postgre_search_path = '' then
         begin
           n := dbm.gf.localprops.ChildNodes.FindNode(dbm.LoginUser);
           if assigned(n)  then dbm.gf.postgre_search_path := vartostr(n.NodeValue);
         end;
         if dbm.gf.postgre_search_path <> '' then
         begin
           q1:= TXDS.Create('control', nil, connection,dbm.gf);
           q1.sqltext := 'SET SEARCH_PATH TO ' + dbm.LoginUser + ',' + dbm.gf.postgre_search_path + ',public';
           q1.execsql;
           FreeAndNil(q1);
         end;
        end;
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        dbm.gf.DisplayTotRows := false;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        //---
        dbm.gf.dodebug.msg('Received XMl ' + s);
        dbm.gf.dodebug.msg('Creating DbCall');
        Validate := TValidate.Create(axprovider);
        x := vartostr(xmldoc.DocumentElement.Attributes['field']);
        v := vartostr(xmldoc.DocumentElement.Attributes['value']);
        q := dbm.GetXDS(nil);
        q.buffered := true;
        q.CDS.CommandText := sqltext;
        result := MakeSearchResultNew(x,v,SearchCond,q,Validate);
        dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg('Error : ' + e.message);
      if pos('access violation',lowercase(e.message)) > 0 then result := CreateErrNode('',false)
      else result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  end;
  GenerateSessionAppKey := False;
  closeproject;
  close_err := '';
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.MakeSearchResultNew(sqlfld,v,SearchCond : String ; q : TXDS; Validate : TValidate) : String;
  var s,tmpsql,sqCol,jsonstr : String;
  k : integer;
  sourcekey : boolean;
  n : ixmlnode;
begin
  result := '';
  jsonstr := '';
  if assigned(q) then
  begin
      n := StructXml.DocumentElement.ChildNodes.FindNode(sqlfld);
      if assigned(n) then SourceKey := n.ChildValues[xml_sourcekey]='True';
      GetPickListResultNew(sqlfld,v,SearchCond,q,validate);
      k := 0;
      if SourceKey then k := 1;
      s := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"},' ;
      jsonstr := WriteResultForGetSearchResultNew(sqlfld, q,k,SourceKey);
  end;
  if jsonstr <> '' then   jsonstr :=  s + jsonstr + ']}'
  else jsonstr := '{"pickdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"}]}' ;
  dbm.gf.dodebug.msg('JSon String : ' + jsonstr);
  result := jsonstr;
end;

procedure TASBTStructObj.GetPickListResultNew(sqlfld,val,SearchCond : String; q : TXDS; Validate : TValidate);
var  i, k : integer;
     SQLString : String;
     NonDynamicFilterSQL : Boolean;
     pickcaptions,pickfields : TStringList;
begin
  pickcaptions := nil;
  pickfields := nil;
  dbm.gf.dodebug.msg('Geting PickList SQL result');
  k:=1;
  pickfields := TStringList.Create;
  pickcaptions := GetPickListFields(sqlfld,q.CDS.CommandText,pickfields);
  NonDynamicFilterSQL := false;
  SQLString := q.CDS.CommandText;
  if pos('dynamicfilter',lowercase(q.CDS.CommandText)) = 0 then
  begin
    NonDynamicFilterSQL := true;
  end
  else if (assigned(pickfields)) and (pickfields.count > 0) then
    SQLString := ChangeSQLNew(q.CDS.CommandText, Val,pickfields,k, SearchCond, '',validate);
  if (pos('{', SQLString) > 0) or (pos('(:', SQLString) > 0) then
      SQLString := FillCompanyParams(SQLString,validate);
  q.CDS.CommandText:=SQLString;
  validate.Parser.RegisterVar(sqlfld,'c',val);
  Validate.DynamicSQL:='';
  if (pickfields.count = 0) and (not NonDynamicFilterSQL) then
    NonDynamicFilterSQL := true;
  Validate.NonDynamicFilterSQL := NonDynamicFilterSQL;
  Validate.AutoQueryOpen(q,1,'0',Val);
  if assigned(pickcaptions) then
  begin
    pickcaptions.Clear;
    FreeAndNil(pickcaptions);
  end;
  if assigned(pickfields) then
  begin
    pickfields.Clear;
    FreeAndNil(pickfields);
  end;
  dbm.gf.dodebug.msg('Geting PickList SQL result over');
end;

function TASBTStructObj.WriteResultForGetSearchResultNew(sqlfld : string ; q:TXDS ;scol :integer;SourceKey : boolean) : String;
var s,val, DepfldList ,depnames: String;
  Idx : Integer;
  DepTrnFldList,DepQryFldList,Dependents : TStringList;
  n : ixmlnode;
begin
  result := '';
  depnames := '';
  DepTrnFldList := nil; DepQryFldList := nil; Dependents := nil;
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
//  if q.CDS.RecordCount = 0 then exit;
  DepTrnFldList := TStringList.Create;
  DepQryFldList := TStringList.Create;
  n := StructXml.DocumentElement.ChildNodes.FindNode(sqlfld);
  if assigned(n) then n:=n.ChildNodes.FindNode(xml_Deps);
  if assigned(n) then
  begin
     Dependents := TStringList.Create;
     Dependents.CommaText := vartostr(n.NodeValue);
  end;
  if assigned(Dependents) then GetAutoCompleteFillDataNew(sqlfld,q,Dependents , DepTrnFldList, DepQryFldList);
  if (assigned(DepQryFldList)) and (assigned(DepTrnFldList)) then
  begin
    for Idx := 0 to DepQryFldList.count -1 do
    begin
       depnames := depnames + DepTrnFldList[Idx] + '^'
    end;
  end;
  s := '';
  q.CDS.First;
  while not q.CDS.Eof do
  begin
    val := '';
    val := q.CDS.Fields[sCol].AsString;
    if pos(#$A,val) > 0  then val := dbm.gf.FindAndReplace(val,#$A,'<br>');
    if pos(#$D,val) > 0  then val := dbm.gf.FindAndReplace(val,#$D,'<br>');
    if pos('"',val) > 0  then val:=dbm.gf.FindAndReplace(val, '"', '^^dq');
    if pos('\',val) > 0  then val:=dbm.gf.FindAndReplace(val, '\', '\\');
    if val <> '' then
    begin
      DepfldList := '';
      if (assigned(DepQryFldList)) and (assigned(DepTrnFldList)) then
      begin
      for Idx := 0 to DepQryFldList.count -1 do
      begin
          DepfldList := DepfldList + q.CDS.FieldByName(DepQryFldList[Idx]).AsString +'^';
      end;
      Delete(DepfldList,length(DepfldList),1);
      if pos(#$D#$A,DepfldList) > 0  then DepfldList := dbm.gf.FindAndReplace(DepfldList,#$D#$A,'<br>');
      if pos(#$A,DepfldList) > 0  then DepfldList := dbm.gf.FindAndReplace(DepfldList,#$A,'<br>');
      if pos(#$D,DepfldList) > 0  then DepfldList := dbm.gf.FindAndReplace(DepfldList,#$D,'<br>');
      DepfldList := dbm.gf.FindAndReplace(DepfldList, '\', '\\');
      DepfldList := dbm.gf.FindAndReplace(DepfldList, '"', '\"');
      If trim(DepfldList) <> '' then DepfldList := '","d":"'+DepfldList;
      end;
      if SourceKey then val := '{"i":"'+val +'","v":"'+ q.CDS.Fields[0].AsString + DepfldList+ '"}'
      else val := '{"i":"'+val +'","v":"'+DepfldList+'"}';
      s := s + val + ',';
    end;
    q.CDS.next;
  end;
  delete(s,length(s),1);
  result := '{"dfname":"'+depnames+'"},{"data":['+ s + ']}' ;
  dbm.gf.dodebug.msg('JSon String : ' + result);
  if assigned(DepTrnFldList) then
  begin
    DepTrnFldList.Clear;
    FreeAndNil(DepTrnFldList);
  end;
  if assigned(DepQryFldList) then
  begin
    DepQryFldList.Clear;
    FreeAndNil(DepQryFldList);
  end;
  if assigned(Dependents) then
  begin
    Dependents.Clear;
    FreeAndNil(Dependents);
  end;
end;

function TASBTStructObj.GetPickListFields(sqlfld,sqltext : string ; pickfields : TStringList) : TStringList;
  var i , j , tpos: integer;
  captions , fnames , f , f1 : string;
  n : ixmlnode;
begin
  result:= nil;
  n := StructXml.DocumentElement.ChildNodes.FindNode(sqlfld);
  if assigned(n) then n:=n.ChildNodes.FindNode(xml_details);
  if assigned(n) then n:=n.ChildNodes.FindNode(xml_sql);
  if assigned(n) then
  begin
    if not ((n.HasAttribute('txt')) and (n.Attributes['txt'] = 't')) then exit;
  end;
  i := pos('dynamicfilter',lowercase(sqlText));
  result:=TStringList.create;
  while i > 0 do
  begin
    captions := copy(sqltext,i,length(sqlText));
    j := pos('}',captions);
    captions := trim(copy(sqlText,i-1,j));
    if pos('dynamicfilterword',lowercase(captions)) > 0 then
      delete(captions,1,18)
    else
      delete(captions,1,14);
    tpos := pos('~',captions);
    if tpos > 0 then begin
     fnames := trim(copy(captions, 1, tpos-1));
     pickfields.Add(fnames);
     captions := Trim(copy(captions,tpos+1,length(captions)));
    end;
    Delete(SQLText,1,i+j);
    i := pos('dynamicfilter',lowercase(sqlText));
  end;
  i := 1;
  while True do
  begin
    f := axprovider.dbm.gf.GetNthString(fnames,i);
    if f = '' then break;
    f1 := axprovider.dbm.gf.GetNthString(captions,i);
    if f1 = '' then f1 := f;
    result.add(f1);
    inc(i);
  end;
end;

procedure TASBTStructObj.GetAutoCompleteFillDataNew(sqlfld : string ;q:TXDS; Dependents : TStringList; var DepTrnFldList: TStringList; var DepQryFldList :TStringList);
var i : Integer;
    Dfld : pFld;
    findField: TField;
    s , dl , sf : string;
    n : ixmlnode;
    Function FieldExistsInCDS(FieldName:String) : Boolean;
    begin
      Result := false;
      findField := nil;
      findField := q.CDS.FindField(FieldName);
      if Assigned(findField) then
        Result := not (findField.Calculated);
    end;
begin
  for i := 0 to Dependents.count-1 do
  begin
    s := Dependents[i];
    if s[1]='f' then
    begin
      n := nil;
      delete(s,1,1);
      n := StructXml.DocumentElement.ChildNodes.FindNode(s);
      if n = nil then continue;
      if assigned(n) then n:=n.ChildNodes.FindNode(xml_details);
      if n = nil then continue;
      dl := vartostr(n.ChildValues[xml_fparent]);
      sf := vartostr(n.ChildValues[xml_fsource]);
      if (lowercase(dl) = lowercase(sqlfld))  and (FieldExistsInCDS(sf)) then
      begin
        DepTrnFldList.Add(s);
        DepQryFldList.Add(sf);
      end;
    end;
  end;
end;

function TASBTStructObj.ChangeSQLNew(SqlText,SearchValue:String;SearchFldList:TStringList;SearchIdx:Integer; SearchCond,SearchValue2 : string; Validate : TValidate):String;
var l, i, j, k:integer;
    replacewith, s, SearchField,tail:String;
begin
   result := sqlText;
   if pos('dynamicfilter',lowercase(sqlText)) = 0 then exit;
   l := length(sqltext);
   if SearchCond = 'starts with' then SearchValue :=  lowercase(SearchValue) + '%'
   else if SearchCond = 'ends with' then SearchValue :=  '%' + lowercase(SearchValue)
   else if SearchCond = 'contains' then SearchValue :=  '%' + lowercase(SearchValue) + '%'
   else if SearchCond = 'between' then SearchValue :=  quotedstr(lowercase(SearchValue)) +' and ' + quotedstr(lowercase(SearchValue2))
   else if SearchCond <> '' then SearchValue :=  lowercase(SearchValue)
   else SearchValue :=  '%' + lowercase(SearchValue) + '%';

   validate.Parser.RegisterVar('axp_dynamicfilter','c',SearchValue);

   k := -1;
   while true do begin
       i := pos('{dynamicfilter',lowercase(sqlText));
       if i=0 then break;
       tail:=copy(sqltext,i,length(sqltext));                              // changed due to picklist not working for companyparams..
       j := pos('}',lowercase(tail));
       inc(k);
       SearchField := dbm.gf.GetNthString(SearchFldList[k],SearchIdx);
       if (SearchCond = 'starts with') or (SearchCond = 'ends with') or (SearchCond = 'contains') or (SearchCond = '') then
         replacewith := dbm.gf.sqllower+'('+searchfield+')' + ' like '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'
       else if (SearchCond = 'between') then
         replacewith := dbm.gf.sqllower+'('+searchfield+')' + ' between '+ dbm.gf.sqllower+'( :axp_dynamicfilter )'
       else
         replacewith := dbm.gf.sqllower+'('+searchfield+') ' + SearchCond + ' '+ dbm.gf.sqllower+'( :axp_dynamicfilter )' ;

       if pos('where', lowercase(SQLText)) > 0 then
         s:=' and '+ replacewith
       else
         s:=' where ' + replacewith;
       delete(sqltext, i, j);
       insert(S, SQLText, i);
   end;
   result := sqlText;
end;

function TASBTStructObj.FillCompanyParams(DynamicSQL : String; Validate : TValidate) : string;
Var
  p1, p2, p, i: integer;
  S, CName, CValue, LastDataType, fname ,val,ParamValue: String;
Begin
  S := DynamicSQL;
  if S = '' then exit;
  While true Do Begin
    p1 := pos('{', S);
    If p1 = 0 Then break;
    p2 := pos('}', S);
    If p2 = 0 Then
      Raise EDataBaseError.Create('Invalid SQL');
    CName := Trim(Copy(S, p1 + 1, p2 - p1 - 1));
    if copy(cName,Length(cName),1) = '*' then begin
      Delete(cName,Length(cName),1);
      ParamValue :=  Validate.GetValue(cName, 1);
      LastDataType := uppercase(Validate.parser.lastvartype);
      If (LastDataType = 'N') then ParamValue := dbm.gf.AppVars.Values[cName];
      if pos('~',paramValue) > 0 then
      begin
        cValue := '';
        i := 1;
        while True do
        begin
          val := axprovider.dbm.gf.GetNthString(paramValue,i,'~');
          if val = '' then break;
          If (LastDataType = 'N') then begin
            if (val = '') Then val := '0' else val := dbm.gf.RemoveCommas(val);
            cValue := cValue+','+ val;
          end else
          begin
            cValue := cValue+','+ quotedstr(val);
          end;
          inc(i);
        end;
        Delete(cValue,1,1);
        if cValue = '' then
        begin
          if LastDataType = 'N' then
            cvalue := '0'
          else
            cValue := QuotedStr(cValue);
        end;
      end else
      begin
        CValue := paramValue;
        if cValue = '' then
        begin
          if LastDataType = 'N' then
            cvalue := '0'
          else
            cValue := QuotedStr(cValue);
        end
        else
        begin
           CValue := QuotedStr(cValue);
        end;
      end;
    end else begin
      CValue :=  Validate.GetValue(cName, 1);
      LastDataType := uppercase(Validate.parser.lastvartype);
      If (LastDataType = 'N') then begin
        if (CValue = '') Then CValue := '0' else CValue := dbm.gf.RemoveCommas(CValue);
      end;
    end;
    dbm.gf.DoDebug.msg(CName + ' = ' + CValue);
    Delete(S, p1, p2 - p1 + 1);
    Insert(CValue, S, p1);
  End;
  result := FillCompositeParams(S,Validate);
End;

Function TASBTStructObj.FillCompositeParams(S: String; Validate : TValidate): String;
Var
  p1,tp1, p2, p: integer;
  CName, CValue, fname: String;
Begin
  p1 := pos('(:', S);
  While p1 > 0 Do Begin
    p2 := pos(')', copy(S,p1+1,20000));
    p2 := p1+p2;
    CName := Copy(S, p1 + 2, p2 - p1 - 2);
    CValue := '(' + Validate.GetValue(CName, -1) + ')';
    If cValue = '()' Then cValue := '('' '')';
    dbm.gf.DoDebug.msg(CName + ' = ' + CValue);
    Delete(S, p1, p2 - p1 + 1);
    Insert(CValue, S, p1);
    p1 := pos('(:', S);
  End;
  Result := S;
End;

function TASBTStructObj.GetMultiSelectValues(s,sxml:string):string;
var x,sqlfld,v,SearchCond,sqltext:String;
    q,q1:TXDS;
    Validate : TValidate;
    n : ixmlnode;
begin
  result := '';
  servicename:='GetMultiSelectValues';
  SearchCond := '';
  try
    dbm := nil;
    dbcall := nil;
    q := nil;
    xmldoc := LoadXMLDataFromWS(s);
    StructXml := LoadXMLDataFromWS(sxml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetMultiSelectValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to GetMultiSelectValues WebService');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to GetMultiSelectValues WebService');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to GetMultiSelectValues WebService');
    if vartype(xmldoc.DocumentElement.Attributes['sqlfield']) = varnull then
      raise Exception.create('field tag not specified in call to GetMultiSelectValues WebService');
    if vartype(xmldoc.DocumentElement.Attributes['value']) = varnull then
      raise Exception.create('value tag not specified in call to GetMultiSelectValues WebService');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    if vartype(xmldoc.DocumentElement.Attributes['cond']) <> varnull then
      SearchCond := lowercase(trim(xmldoc.DocumentElement.Attributes['cond']));
    sqltext := vartostr(xmldoc.DocumentElement.ChildNodes.FindNode('sqltext').NodeValue);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := False;
    ValidateSessionAppKey := False;
    SetDefaultValues := False;
    GetServerDT := False;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing GetMultiSelectValues webservice');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
        if dbm.Connection.DbType = 'postgre' then
        begin
         if dbm.gf.postgre_search_path = '' then
         begin
           n := dbm.gf.localprops.ChildNodes.FindNode(dbm.LoginUser);
           if assigned(n)  then dbm.gf.postgre_search_path := vartostr(n.NodeValue);
         end;
         if dbm.gf.postgre_search_path <> '' then
         begin
           q1:= TXDS.Create('control', nil, connection,dbm.gf);
           q1.sqltext := 'SET SEARCH_PATH TO ' + dbm.LoginUser + ',' + dbm.gf.postgre_search_path + ',public';
           q1.execsql;
           FreeAndNil(q1);
         end;
        end;
        //-- for pagination
        dbm.gf.pagination_pageno := 0;
        dbm.gf.DisplayTotRows := false;
        if vartype(xmldoc.DocumentElement.Attributes['pageno']) <> varnull then
           dbm.gf.pagination_pageno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pageno']));
        if vartype(xmldoc.DocumentElement.Attributes['pagesize']) <> varnull then
           if vartostr(xmldoc.DocumentElement.Attributes['pagesize']) <> '' then
              dbm.gf.pagination_pagesize := strtoint(vartostr(xmldoc.DocumentElement.Attributes['pagesize']));
        //---
        dbm.gf.dodebug.msg('Received XMl ' + s);
        dbm.gf.dodebug.msg('Creating DbCall');
        Validate := TValidate.Create(axprovider);
        x := vartostr(xmldoc.DocumentElement.Attributes['field']);
        v := vartostr(xmldoc.DocumentElement.Attributes['value']);
        q := dbm.GetXDS(nil);
        q.buffered := true;
        q.CDS.CommandText := sqltext;
        result := MakeMultiSelectList(x,v,SearchCond,q,Validate);
        dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      dbm.gf.dodebug.msg('Error : ' + e.message);
      if pos('access violation',lowercase(e.message)) > 0 then result := CreateErrNode('',false)
      else result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  end;
  GenerateSessionAppKey := False;
  closeproject;
  close_err := '';
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.MakeMultiSelectList(sqlfld,v,SearchCond : String ; q : TXDS; Validate : TValidate) : String;
  var s,tmpsql,sqCol,jsonstr : String;
  k : integer;
  sourcekey : boolean;
  n : ixmlnode;
begin
  result := '';
  jsonstr := '';
  if assigned(q) then
  begin
      n := StructXml.DocumentElement.ChildNodes.FindNode(sqlfld);
      if assigned(n) then SourceKey := n.ChildValues[xml_sourcekey]='True';
      GetPickListResultNew(sqlfld,v,SearchCond,q,validate);
      k := 0;
      if SourceKey then k := 1;
      s := '{"multiselectdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"},' ;
      jsonstr := WriteResultForGetMultiSelectValues(sqlfld, q,k,SourceKey);
  end;
  if jsonstr <> '' then   jsonstr :=  s + jsonstr + ']}'
  else jsonstr := '{"multiselectdata":[{"rcount":"'+inttostr(dbm.gf.pagination_totalrows)+'"},{"fname":"'+sqlfld+'"}]}' ;
  dbm.gf.dodebug.msg('JSon String : ' + jsonstr);
  result := jsonstr;
end;

function TASBTStructObj.WriteResultForGetMultiSelectValues(sqlfld : string ; q:TXDS ;scol :integer;SourceKey : boolean) : String;
var s,val, DepfldList ,depnames,fn,fv: String;
  Idx,i : Integer;
  DepTrnFldList,DepQryFldList,Dependents : TStringList;
  n : ixmlnode;
begin
  result := '';
  depnames := '';
  DepTrnFldList := nil; DepQryFldList := nil; Dependents := nil;
  dbm.gf.dodebug.msg('CDS Record Count : ' + inttostr(q.CDS.RecordCount));
//  if q.CDS.RecordCount = 0 then exit;
  DepTrnFldList := TStringList.Create;
  DepQryFldList := TStringList.Create;
  n := StructXml.DocumentElement.ChildNodes.FindNode(sqlfld);
  if assigned(n) then n:=n.ChildNodes.FindNode(xml_Deps);
  if assigned(n) then
  begin
     Dependents := TStringList.Create;
     Dependents.CommaText := vartostr(n.NodeValue);
  end;
  if assigned(Dependents) then GetAutoCompleteFillDataNew(sqlfld,q,Dependents , DepTrnFldList, DepQryFldList);
  if (assigned(DepQryFldList)) and (assigned(DepTrnFldList)) then
  begin
    for Idx := 0 to DepQryFldList.count -1 do
    begin
       depnames := depnames + DepTrnFldList[Idx] + '^'
    end;
  end;
  s := '';
  q.CDS.First;
  while not q.CDS.Eof do
  begin
    s := s + '{';
    for i:=0 to q.CDS.FieldCount-1 do begin
      if lowercase(q.CDS.Fields[i].FieldName) = 'axrnum' then continue;   // to skip axrnum column which is used for pagination.
      fn := lowercase(q.CDS.Fields[i].FieldName);
      if q.CDS.fields[i].text = '' then
        val := ''
      else
        val := q.CDS.Fields[i].AsString;
      if pos(#$A,val) > 0  then val := dbm.gf.FindAndReplace(val,#$A,'<br>');
      if pos(#$D,val) > 0  then val := dbm.gf.FindAndReplace(val,#$D,'<br>');
      if pos('"',val) > 0  then val:=dbm.gf.FindAndReplace(val, '"', '^^dq');
      if pos('\',val) > 0  then val:=dbm.gf.FindAndReplace(val, '\', '\\');
      if (i = sCol) and (val <> '') then
      begin
        DepfldList := '';
        if (assigned(DepQryFldList)) and (assigned(DepTrnFldList)) then
        begin
        for Idx := 0 to DepQryFldList.count -1 do
        begin
            DepfldList := DepfldList + q.CDS.FieldByName(DepQryFldList[Idx]).AsString +'^';
        end;
        Delete(DepfldList,length(DepfldList),1);
        DepfldList := dbm.gf.FindAndReplace(DepfldList, '\', '\\');
        DepfldList := dbm.gf.FindAndReplace(DepfldList, '"', '\"');
        If trim(DepfldList) <> '' then DepfldList := '","d":"'+DepfldList;
        end;
        if SourceKey then val := '"' + fn +'":"'+val +'","v":"'+ q.CDS.Fields[0].AsString + DepfldList+ '"'
        else val := '"' + fn +'":"'+val +'","v":"'+DepfldList+'"';
        s := s + val ;
      end else
      begin
        val := '"' + fn +'":"'+ val +'"';
        s := s + val ;
      end;
      s := s + ',';
    end;
    delete(s,length(s),1);
    s := s + '},';
    q.CDS.next;
  end;
  delete(s,length(s),1);
  result := '{"dfname":"'+depnames+'"},{"data":['+ s + ']}' ;
  dbm.gf.dodebug.msg('JSon String : ' + result);
  if assigned(DepTrnFldList) then
  begin
    DepTrnFldList.Clear;
    FreeAndNil(DepTrnFldList);
  end;
  if assigned(DepQryFldList) then
  begin
    DepQryFldList.Clear;
    FreeAndNil(DepQryFldList);
  end;
  if assigned(Dependents) then
  begin
    Dependents.Clear;
    FreeAndNil(Dependents);
  end;
end;

function TASBTStructObj.DoFormLoadNew(xml, sxml , formload_fldlist : string):string;
var x,fldlist,fglist,loadflag,s,nonsaveflds,tmpfldlist,formLoadDBTransids , config_data : String;
    loadstruct,actflag : boolean;
    recid:extended;
    n , fldnode : ixmlnode;
    stime:TDateTime;
    i , j : integer;
    fd : pFld;
begin
  result := '';
  servicename:='Quick Form Load';
  FastDataFlag := true;
  stime:=now;
  try
    if xml = '' then
      raise Exception.create('Input Data not available to execute formload web service...');
    xmldoc := LoadXMLDataFromWS(xml);

    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    actflag := false;
    if xmldoc.documentelement.HasAttribute('act') then
    begin
       if vartostr(xmldoc.documentelement.attributes['act']) <> '' then
          actflag := true;
    end;
    if not actflag then
       if loadflag = '' then raise Exception.create('Quick form load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);

    dbm := nil;
    if sxml = '' then
      raise Exception.create('Structure XML not available to execute formload web service...');
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice DoFormLoad');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DoFormLoad');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);

    if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
    begin
       VisibleDCs := vartostr(xmldoc.DocumentElement.Attributes['dcname']);
    end;
    if not actflag then
    begin
      if (loadflag = 'false') then
      begin
        if (fglist <> '') and (VisibleDCs <>'') then
        begin
          i := 1;
          while true do
          begin
            s := GetnthString(VisibleDCs,i);
            if s = '' then break;
            if pos(s,fglist) > 0 then
            begin
              loadflag := 'true';
              break;
            end;
            i := i + 1;
          end;
        end;
      end;
      if loadflag = 'false' then raise Exception.create('Quick form load Structure XML not valid...');
    end;
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick DoFormLoad webservice');
    dbm.gf.dodebug.msg('-------------------------------------');
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      dbm.gf.dodebug.msg('Received XML ' + xml);
      dbm.gf.dodebug.msg('Received SXML ' + sxml);

      x:= xmldoc.DocumentElement.Attributes['transid'];
      if xmldoc.documentelement.HasAttribute('act') and (vartostr(xmldoc.documentelement.attributes['act']) = 'load') then
         ASBDataObj.CreateAndSetDbCall(x,sxml)
      else ASBDataObj.CreateAndSetDbCall('fload',x,fldlist,visibleDCs,fglist,'',StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.DataSubmitSort := false;
      ASBDataObj.SubmitClientValuesToSD_New;
      //----
      formLoadDBTransids := dbCall.Parser.GetVarValue('forms_transids');
      if formLoadDBTransids <> '' then
      begin
         if pos(','+x+',',formLoadDBTransids) > 0 then
            ASBCommonObj.GetDBVarsForFormsAndReports(x,'tstruct',dbcall.Parser);
      end;
      config_data := '';
      formLoadDBTransids := dbCall.Parser.GetVarValue('configparam_transids');
      if formLoadDBTransids <> '' then
      begin
         if pos(','+x+',',formLoadDBTransids) > 0 then
            config_data := ASBCommonObj.GetConfigParamForFormsAndReports(x,dbcall.Parser);
      end;
      //----
      ASBDataObj.CreateActionObj(x,'tstructs');
      loadstruct := true;
      recid := 0;
      if xmldoc.documentelement.HasAttribute('loadformdata') then
      begin
         if (vartostr(xmldoc.documentelement.attributes['loadformdata']) = 'true') then
         begin
           if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
           recid := 0;
           if (tcstr <> 'n') then
           begin
            n := xmldoc.DocumentElement;
            recid := GetRecordIDtoLoad(n);
            if recid = 0  then  raise Exception.Create('No data found for the given parameter values...');
           end;
         end;
      end;
      if xmldoc.documentelement.HasAttribute('act') then
      begin
         if (vartostr(xmldoc.documentelement.attributes['act']) = 'load') then
         begin
           if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
           if (tcstr <> 'n') then
           begin
            n := xmldoc.DocumentElement;
            if xmldoc.documentelement.HasAttribute('loadformdata') then
            begin
              if (vartostr(xmldoc.documentelement.attributes['loadformdata']) = 'false') then recid := 0;
            end else recid := GetRecordIDtoLoad(n);
           end;
         end;
         if (vartostr(xmldoc.documentelement.attributes['act']) = 'open') or (recid = 0) then
         begin
            loadstruct := false;
            tmpfldlist := ',' + fldList +',' ;
            for i := 0 to xmldoc.documentelement.ChildNodes.Count-1 do
            begin
              s := vartostr(xmldoc.documentelement.ChildNodes[i].NodeName);
              if pos(','+s+',' , tmpfldlist) = 0 then
              begin
                 fldnode := StructXml.DocumentElement.ChildNodes.FindNode(s);
                 if assigned(fldnode) then
                 begin
                   dbcall.struct.GetFrameForWebLoad(vartostr(fldnode.Attributes['dcno']));
                   dbcall.struct.xload_selected_field(fldnode);
                 end;
                 fldlist := fldlist + ',' + s;
              end;
              fd := DbCall.Struct.GetField(s);
              if assigned(fd) then
              begin
                if fd.ModeofEntry ='select' then
                begin
                  s := AddFillFldToList(StructXml,s);
                  fldlist := fldlist + s;
                end;
              end;
            end;
            ASBDataObj.imagefromdb := true;
         end;
      end;
      if xmldoc.documentelement.ChildNodes.Count = 0 then loadstruct := false
      else ASBDataObj.RefreshGrid := True;
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      stime:=now;
      ASBDataObj.act.VisibleDCs := ASBDataObj.VisibleDCs;
      if loadstruct = false then
      begin
        dbCall.Parser.RegisterVar('ActiveRow', 'n', '1');
        ASBDataObj.FormLoadFldList := fldlist;
        if xmldoc.documentelement.ChildNodes.Count > 0 then
        begin
          dbCall.struct.MakeProperStartIndex;
          {
          dbm.gf.dodebug.msg('After submit, checking rows in grid');
          for i := 0 to xmldoc.documentelement.ChildNodes.Count-1 do
          begin
            s := vartostr(xmldoc.documentelement.ChildNodes[i].NodeName);
            fd := DbCall.Struct.GetField(s);
            if assigned(fd) then
            begin
              fm := pFrm(dbcall.struct.frames[fd.FrameNo-1]);
              if assigned(fm) and (fm.AsGrid) and (fd.DataRows.Count > 0) then
              begin
                 fm.HasDataRows := True;
                 s := 'true';
              end else s := 'false';
              dbm.gf.dodebug.msg('Dc No As Grid' + inttostr(fd.FrameNo) + ' - ' + s);
              if not fm.HasDataRows then
              begin
                 dbm.gf.dodebug.msg('Node Value : ' + vartostr(xmldoc.documentelement.ChildNodes[i].NodeValue));
                 s := vartostr(xmldoc.documentelement.ChildNodes[i].NodeValue);
                 if s <>  '' then
                 begin
                   dbcall.StoreData.DirectSubmitValue(fd, s, '', 1, 0, 0, 0);
                   fm.HasDataRows := True;
                   dbm.gf.dodebug.msg('Dc No val after new submit : ' + inttostr(fd.FrameNo) + ' - ' + booltostr(fd.AsGrid));
                 end;
              end;
            end;
          end;
          dbm.gf.dodebug.msg('After submit, checking rows in grid over');
          }
        end;
        FindAndExeFormLoadAction ;
        fldlist := ASBDataObj.FormLoadFldList;
        if ASBDataObj.LoadDataByAction then
        begin
          nonsaveflds := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['nonsaveflds']);
          PrepareNonSaveFlds(nonsaveflds);
          FindAndExeDataLoadAction;
          dbCall.struct.MakeProperStartIndex;
//          dbcall.Struct.flds.sort(sortfldsDefault);
          LoadDataJSONNew;
          Result := ASBDataObj.GetJSON;
        end else
        begin
          if (ASBDataObj.act.checkFillGrid<>'') then
             dbCall.struct.CreateSelectedFillGridDCOnDemand(ASBDataObj.act.checkFillGrid,fglist);
          dbCall.struct.MakeProperStartIndex;
          dbcall.Struct.flds.sort(sortfldsDefault);
          ASBDataObj.VisibleDCs := ASBDataObj.act.VisibleDCs;
          VisibleDCs := ASBDataObj.VisibleDCs;
          if dbCall.VisibleDCs <> '' then
             VisibleDCs := VisibleDCs + dbCall.VisibleDCs;
          NewFormLoadJSON(fldlist);
          Result := ASBDataObj.GetJSON;
          s := GlobalVarsInSqls ;
          s := '{"globalVars":[' + s +  ']}';
          result := result + '*$*' + s ;
        end;
      end else
      begin
         ASBDataObj.RefreshGrid := False;
         nonsaveflds := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['nonsaveflds']);
         CreateNonSaveFlds(nonsaveflds);
         dbm.gf.dodebug.msg('Record ID : ' + floattostr(recid));
         dbcall.LoadDataForWeb(recid);
         ASBDataObj.act.RecId := floattostr(recid);
         PrepareNonSaveFlds(nonsaveflds);
         FindAndExeFormLoadAction;
         FindAndExeDataLoadAction;
         dbCall.struct.MakeProperStartIndex;
//         dbcall.Struct.flds.sort(sortfldsDefault);
         LoadDataJSONNew;
         Result := ASBDataObj.GetJSON;
      end;
      if result <> '' then
      begin
        if dbm.gf.AxMemVars <> '' then result := result + dbm.gf.AxMemVars;
        if config_data <> '' then result := result + '$*$' + config_data;

      end;
      dbm.gf.dodebug.msg('Result : ' + Result);
      dbm.gf.dodebug.msg('Time taken by Quick FormLoad - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(ASBCommonObj) then ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      if assigned(dbm) then dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  if assigned(dbm) then
  begin
    dbm.gf.dodebug.msg('Quick DoFormLoad webservice completed');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('');
    x := CloseProject;
    if result = '' then  result := x
    else result := x + '*$*' +  result;
  end;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.AddFillFldToList(xml : IXMLDocument ; pfld : ansistring) : AnsiString;
  var i : integer;
  n , dnode : ixmlnode;
  cat : ansistring;
begin
  result := '';
  for i:=1 to xml.DocumentElement.ChildNodes.Count-1 do
  begin
    n:=xml.DocumentElement.ChildNodes[i];
    if n.NodeName='iframes' then continue;
    cat:=vartostr(n.attributes['cat']);
    if cat='field' then
    begin
      if lowercase(n.ChildValues[xml_moe]) = 'fill' then
      begin
         dnode:=n.ChildNodes.FindNode(xml_details);
         if vartostr(dnode.ChildValues[xml_fparent]) = pfld then
         begin
            dbcall.struct.GetFrameForWebLoad(vartostr(n.Attributes['dcno']));
            dbcall.struct.xload_selected_field(n);
            result := result  + ',' + n.NodeName;
         end;
      end;
    end;
  end;
end;


procedure TASBTStructObj.NewFormLoadJSON(fldlist : string);
var i, PriorFrame, PriorRow, RCount, j,k,prcount  : integer;
    fld:pFld;
    frec:string;
    stime:TDateTime;
    fg:pFg;
    WithDropDown , dcrefresh : Boolean;
    fm : pFrm;
    popgrid : ppopgrid;
    v : string;
begin
  dbm.gf.dodebug.msg('Quick Form Load JSON');
  Dbcall.validate.FormLoadPrepareField := true;
  dbcall.struct.FldLoadedDuringStructLoad := false;
  stime:=now;
  asbDataObj.JSONStr:='';
  PriorRow:=0; PriorFrame:=0;
  fg:=nil;
  i:=0;
  fldList := ','+fldlist+',';
  while i<dbcall.struct.flds.Count do begin
    fld := pfld(dbcall.struct.flds[i]);
    WithDropDown := false;

    if priorframe <> fld.frameno then begin
      if (priorframe <> 0) then
         AsbDataObj.QuickChangeHasDataRowStatus(dbcall.Struct.GetFrame(priorframe));
      if (ASBDataObj.act.checkFillGrid<>'') and ((pos(quotedstr('dc' + inttostr(fld.FrameNo)),ASBDataObj.act.checkFillGrid) > 0)) then
          WithDropDown := false
      else if (dbcall.struct.FgAutoShowMapFlds <> ',') and (pos(','+fld.FieldName+',' , dbcall.struct.FgAutoShowMapFlds) > 0)then
           WithDropDown := false
      else if (pos(','+fld.FieldName+',' , fldList) = 0) then
      begin
        inc(i);
        continue;
      end;
      RCount := dbcall.StoreData.GetRowCount(i);
      if RCount = 0  then  RCount:=1;
      PriorFrame := fld.FrameNo;
      PriorRow:=0;
      dbCall.Parser.RegisterVar('ActiveRow', 'n', '1');
      fg:=ExecFillGrid(fld.FrameNo, fgFromFormLoad,'');
      if assigned(fg) then begin
        dcrefresh := ASBDataObj.RefreshGrid;
        ASBDataObj.RefreshGrid := False;
        i:=AsbDataObj.GridToJSONNew(fld.FrameNo, WithDropDown);
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields,dbcall.Struct.GetFrame(fld.FrameNo));
        if assigned(fg.q) then dbm.gf.GetUsedGlobalVarsInSqlParams(fg.q.CDS);
        ASBDataObj.RefreshGrid := dcrefresh;
        fg:=nil;
        continue;
      end;
      fm:=dbcall.Struct.GetFrame(fld.FrameNo);
      if fm.Popup then
      begin
        popgrid:=nil;
        for j := 0 to dbcall.struct.popgrids.count-1 do begin
          if pPopGrid(dbcall.struct.popgrids[j]).FrameNo=fld.FrameNo then begin
            popgrid:=pPopGrid(dbcall.struct.popgrids[j]);
            break;
          end;
        end;
        if not assigned(popgrid) then
        begin
          i :=fm.StartIndex+fm.FieldCount;
          continue;
        end;
        rcount:=dbcall.storedata.RowCount(popgrid.ParentFrameNo);
        if rcount > 0 then
        begin
          prcount := dbcall.storedata.RowCount(popgrid.FrameNo);
          asbDataObj.PopDCToJSON(fld.FrameNo, prcount);
          for j := 1 to rcount do
          begin
            dbcall.validate.RegRow(popgrid.ParentFrameNo, j);
            i := AsbDataObj.PopGridToJSON(popgrid, j, true);
          end;
        end;
      end;
    end;

    if (fld.DataRows.count=0 ) and (not fld.AsGrid) then
        if (fld.ModeOfEntry='select') and (not fld.FromList) and (fld.ComponentType<>'')  then
            frec:=Dbcall.validate.Prepare_Selected_Field(fld,'loaddata')
        else frec:=Dbcall.validate.Prepare_Selected_Field(fld,VisibleDCs)
    else begin
      if fld.AsGrid then begin
        if ((visibleDCs<>'') and ((pos(quotedstr('dc' + inttostr(fld.FrameNo)),VisibleDCs) = 0))) or (ASBDataObj.RefreshGrid) then
        begin
          dcrefresh := ASBDataObj.RefreshGrid;
          ASBDataObj.RefreshGrid := False;
          i:=AsbDataObj.GridToJSONNew(fld.FrameNo, WithDropDown);
          ASBDataObj.RefreshGrid := dcrefresh;
          continue;
        end else
        begin
          if assigned(fld.QSelect) then dbm.gf.GetUsedGlobalVarsInSqlParams(fld.QSelect.CDS);
          if fld.cexp<>'' then dbm.gf.GetUsedGlobalVarsInExpr(fld.cexp);
          inc(i);
          continue;
        end;
      end else begin
        frec:=Dbcall.validate.Prepare_Selected_Field(fld,VisibleDCs);
        v := dbm.gf.GetNthString(frec,1,'~');
        if (v = '') and (fld.cexp = '') then
        begin
           v:=Dbcall.validate.GetValue(fld.FieldName , 1);
           frec:= v + '~1~0~0';
        end;
      end;
    end;
    if fld.DataType = 'i' then begin
      if fld.Tablename <> '' then
      begin
        asbDataObj.ImageFieldToJSON(fld) ;
        inc(i);
        continue;
      end else
      begin
        If (fld.Exprn >= 0) Then Begin
          If Dbcall.Parser.EvalPrepared(fld.Exprn) Then
          begin
            v := Dbcall.Parser.Value;
            if v <> '' then
            begin
              asbDataObj.ImageFieldToJSON(fld,v);
              inc(i);
              continue;
            end;
          end;
        end;
      end;
    end;
    inc(i);
    asbDataObj.FieldToJSONNew(fld, frec);
  end;
  AsbDataObj.QuickChangeHasDataRowStatus(dbcall.Struct.GetFrame(priorframe));
  asbDataObj.EndDataJSON;
  dbm.gf.dodebug.msg('Quick Form load JSON completed '+inttostr(millisecondsbetween(now, stime)));
end;


function TASBTStructObj.GetDepentendFieldValuesNew(xml,sxml,formload_fldlist:string):string;
var x,fldname,s,loadflag,fldlist,fglist,dlist,depfldlist,f,depfld,ParentList,fldtype,tmpdepfldlist,tmpdc,srecid : String;
    i,j,k,actrow,pactrow : integer;
    dcload : boolean;
    fnode,n : ixmlnode;
    fd:pFld;
    recid : extended;
begin
  result := '';
  servicename:='Quick Get Dependents';
  try

    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);

    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetDepentendFieldValues') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice GetDepentendFieldValues');
    if vartype(xmldoc.DocumentElement.Attributes['field']) = varnull then
      raise Exception.create('field tag not specified in call to webservice GetDepentendFieldValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);

    //----
    depfldlist := '';
    dcload:=false;
    fldname := xmldoc.DocumentElement.Attributes['field'];
    fnode := StructXML.DocumentElement.ChildNodes.FindNode(fldname);
    if fnode <> nil then
    begin
       tmpdc := 'dc' + vartostr(fnode.Attributes['dcno']);
       i := pos(tmpdc,fglist);
       if i > 0 then delete(fglist,1,i+1);
       n := fnode.ChildNodes.FindNode('a58');
    end;
    if n <> nil then
    begin
      if n.IsTextElement then
      begin
        dlist := VarToStr(n.NodeValue);
        i := 1;
        s := GetnthString(dlist,i);
        while s <> '' do
        begin
          fldtype := copy(s,1,1);
          depfld := copy(s, 2, 100);
          if not dcload then
          begin
            k := 1;
            while true do
            begin
              f := GetnthString(formload_fldlist,k);
              if f = '' then break;
              j := pos('`',f);
              f := copy(f,j+1,length(f));
              if (depfld = f) or (fldtype='d') or (fldtype='g') then
              begin
                dcload := true;
                break;
              end;
              inc(k);
            end;
          end;
          if pos(',' + depfld + ',',tmpdepfldlist) > 0 then
          begin
            inc(i);
            s := GetnthString(dlist,i);
            continue;
          end;
          if (fldtype<>'d') and (fldtype<>'g') and (fldtype<>'$') then
          begin
            tmpdepfldlist := tmpdepfldlist + ',' + depfld + ',';
            depfldlist := depfldlist + ',' + depfld;
          end else dcload := true;
          inc(i);
          s := GetnthString(dlist,i);
        end;
      end else
    end else dcload := true;
    if not dcload then
    begin
       if depfldlist <> '' then dcload := true;
    end;
    if not dcload then raise Exception.create('No Dependant field to refresh...');
    delete(depfldlist,1,1);
    //---
    if fnode <> nil then n := fnode.ChildNodes.FindNode('a66');
    if n <> nil then
    begin
      if n.IsTextElement then
      begin
        ParentList := VarToStr(n.NodeValue);
        if pos('~',ParentList) > 0 then
        begin
           ParentList := copy(ParentList,1,pos('~',ParentList)-1);
        end;
      end;
    end;
//    delete(fldlist,1,pos(fldname+',' , fldlist)+ length(fldname));
    if ParentList <> '' then
       fldlist := ParentList + ',' + depfldlist + ',' + fldlist
    else begin
      fldlist := depfldlist + ',' + fldlist;
      if pos(','+fldname+',' , fldlist) = 0 then fldlist := fldname + ',' +  fldlist;
    end;
    //----

    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick GetDepentendFieldValues webservice');
    dbm.gf.dodebug.msg('--------------------------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession ;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartype(xmldoc.DocumentElement.Attributes['imagefromdb']) <> varnull then
        if vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']) <> '' then
           ASBDataObj.imagefromdb := strtobool(vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']));
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;
      x:= xmldoc.DocumentElement.Attributes['transid'];

      dbm.gf.dodebug.msg('Creating DBCall');
      ASBDataObj.CreateAndSetDbCall('dep',x,fldlist,visibleDCs,fglist,'',StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.depCall := true;
      ASBDataObj.SubmitClientValuesToSD_New;
      ASBDataObj.CreateActionObj(x,'tstructs');
      dbCall.struct.visibleDCs := visibleDCs;
      dbCall.StoreData.depCall := ASBDataObj.depCall;

      actrow := 0 ;
      if vartype(xmldoc.DocumentElement.Attributes['activerow']) <> varnull then
         actrow := strtoint(vartostr(xmldoc.DocumentElement.Attributes['activerow']));
      if actrow = 0 then actrow := 1;
      if vartype(xmldoc.DocumentElement.Attributes['prow']) <> varnull then
      begin
         if vartostr(xmldoc.DocumentElement.Attributes['prow']) <> '' then
            pactrow := strtoint(vartostr(xmldoc.DocumentElement.Attributes['prow']));
         dbCall.Parser.RegisterVar('activeprow', 'n', inttostr(pactrow));
      end;
      if vartype(xmldoc.DocumentElement.Attributes['recordid']) <> varnull then
      begin
         srecid := vartostr(xmldoc.DocumentElement.Attributes['recordid']);
         recid := dbm.gf.strtofloatz(srecid);
         if recid >= 0 then
         begin
            dbm.gf.dodebug.msg('Record Id : ' + srecid);
            DbCall.Parser.RegisterVar('recordid', 'n', srecid);
            dbcall.Validate.Parser.RegisterVar('recordid', 'n', srecid);
         end;
      end;
      if result = '' then
      begin
        fd:=DbCall.Struct.GetField(fldname);
        if assigned(fd) then
        begin
          GetDependentsNew(actrow,fd);
          result:=ASBDataObj.GetJSON;
        end;
      end;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(ASBCommonObj) then ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      if assigned(dbm) then dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  if assigned(dbm) then
  begin
    dbm.gf.dodebug.msg('Executing Quick GetDepentendFieldValues webservice over');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('');
  end;
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

Procedure TASBTStructObj.GetDependentsNew( RowNo:integer;fd:pFld);
var dfd,fldtmp:pFld;
    fm, dfm,tfm:pFrm;
    frec:pFieldRec;
    fg:pFg;
    i, j, actrow,k:integer;
    DCInJSON, DCNum, NewValue, S , v , value: String;
    IdValue : Extended;

    procedure DCTOJSON(jfd:pFld);
    var rcount:integer;
    begin
      if pos(DCNum, DCInJSON)>0 then exit;
      tfm:=dbcall.Struct.GetFrame(jfd.FrameNo);
      if tfm.jsonstr <> '' then exit;
      RCount := dbcall.StoreData.RowCount(jfd.FrameNo);
      if actrow > -1 then
         asbDataObj.DCToJSON(jfd.FrameNo,RCount,actrow)
      else asbDataObj.DCToJSON(jfd.FrameNo,RCount);
      DCInJSON:=DCInJSON+','+inttostr(jfd.FrameNo)+',';
    end;
begin
  dbm.gf.DoDebug.msg('Refreshing Dependents');
  if not assigned(fd.Dependents) then exit;

  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  if fm.PopIndex > -1 then fm.HasDataRows := true;  // This is send dummy row to client side id getdependency call comes from popgrid field.

  DCInJSON:='';
  asbDataObj.jsonstr:='';
  actrow := -1;
  if fd.asgrid then begin
    dbcall.Validate.RegRow(fd.FrameNo, RowNo);
    actrow := RowNo;
    DCTOJSON(fd);
    actrow := -1;
    if (fd.PopIndex>-1) and (fd.IsParentField) and (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
      dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
    RefreshRowNew(fd, RowNo);
    dbCall.RefreshGridRowDependents(fm,fd);
    AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
  end else begin
    for I := 0 to fd.Dependents.count-1 do begin
      dbm.gf.DoDebug.msg('Refreshing '+fd.Dependents[i]+ ' Type '+fd.DependentTypes[i+1]);
      if fd.DependentTypes[i+1]='d' then begin
        DCTOJSON(fd);
        s:=fd.Dependents[i];
        if lowercase(copy(s,1,2)) = 'dc' then delete(s,1,2);
        DCNum:=','+s+',';
        if (pos(dcnum, DCInJSON)>0) then continue;
        if (fd.PopIndex>-1) and (fd.IsParentField) and (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
           dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
        DCInJSON:=DCInJSON+DoPopup(StrToInt(s), RowNo);
        continue;
      end else if fd.DependentTypes[i+1]='g' then begin
        DCTOJSON(fd);
        s:=fd.Dependents[i];
        delete(s,1,2);
        DCInJSON:=DCInJSON+DepFillGridNew(StrToInt(s),fd.Dependents);
        continue;
      end;
      dfd:=dbcall.struct.GetField(fd.dependents[i]);
      if not assigned(dfd) then continue;
      if (dfd.ModeofEntry = 'select') and ((not dfd.Autoselect) and (dfd.cexp='')) then
      begin
        if (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
        begin
          DCToJSON(dfd);
          ASBDataObj.FieldToJSONNew(dfd, '`'+ inttostr(RowNo) +' `0`0');
          dbcall.StoreData.SubmitValue(dfd.FieldName, RowNo, '', '', 0, 0, 0);
          dbcall.Parser.RegisterVar(dfd.FieldName,Char(dfd.DataType[1]),'');
          continue;
        end;
      end;
      dfm:=dbcall.Struct.GetFrame(dfd.FrameNo);
      DCNum:=','+inttostr(dfd.FrameNo)+',';
      if (dfd.AsGrid) and (pos(dcnum, DCInJSON)>0) then continue;
      if (not fd.AsGrid) and (not dfd.AsGrid) then begin
        if dfd.DataType = 'i' then begin
          if dfd.Tablename <> '' then
            asbDataObj.ImageFieldToJSON(dfd)
          else begin
            If (dfd.Exprn >= 0) Then Begin
              If Dbcall.Parser.EvalPrepared(dfd.Exprn) Then
              begin
                v := Dbcall.Parser.Value;
                if v <> '' then
                  asbDataObj.ImageFieldToJSON(dfd,v)
              end;
            end;
          end;
          continue;
        end;
        if (dfd.ModeofEntry = 'autogenerate') then begin
          if DbCall.StoreData.LastSavedRecordId = 0 then
            value := DbCall.Validate.RefreshField(dfd, rowno,true)
        end else
          value :=  DbCall.Validate.RefreshField(dfd, rowno,true);
        DCToJSON(dfd);
        ASBDataObj.FieldToJSONNew(dfd, value);
      end else if (fd.AsGrid) and (not dfd.AsGrid) then begin
        if (dfd.ModeofEntry = 'autogenerate') then begin
          if DbCall.StoreData.LastSavedRecordId = 0 then
            value := DbCall.Validate.RefreshField(dfd, 1,true);
        end else
          value := DbCall.Validate.RefreshField(dfd, 1,true);
        DCToJSON(dfd);
        ASBDataObj.FieldToJSONNew(dfd, value);
      end else if (not fd.AsGrid) and (dfd.AsGrid) then begin
        DCInJSON:=DCInJSON+','+inttostr(dfd.FrameNo)+','+NonGridToGridNew(fd, dfd);;
      end else if (fm.popindex>-1) and (dfd.AsGrid) then begin
        frec:=dbcall.storedata.getfieldrec(fd, RowNo);
        value := DbCall.Validate.RefreshField(dfd, frec.ParentRowNo);
        DCTOJSON(dfd);
        ASBDataObj.FieldToJSONNew(dfd, value);
      end;
    end;
  end;
  asbDataObj.EndDataJSON;
end;

procedure TASBTStructObj.RefreshRowNew(fd:pFld; RowNo:Integer);
var r, j, rcount, PopIndex,k:integer;
    fm:pFrm;
    fld,fldtmp:pfld;
    value, ov , val : string;
begin
  dbm.gf.DoDebug.msg('Refreshing row '+IntToStr(RowNo)+' in frame '+IntToStr(fd.frameno));
  fm:=dbcall.Struct.GetFrame(fd.FrameNo);
  for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
    fld:=pFld(dbcall.struct.flds[j]);
    if not assigned(fld) then continue;
    if fd.FieldName = fld.FieldName then continue;
    if fd.Dependents.IndexOf(fld.FieldName)=-1 then continue;
    if (fld.ModeofEntry = 'select') and ((not fld.Autoselect) and (fld.cexp='')) then
    begin
      if (dbcall.StoreData.GetFieldValue(fd.FieldName,rowno) = '') then
      begin
        ASBDataObj.FieldToJSONNew(fld, '`'+ inttostr(RowNo) +' `0`0');
        dbcall.StoreData.SubmitValue(fld.FieldName, RowNo, '', '', 0, 0, 0);
        dbcall.Parser.RegisterVar(fld.FieldName,Char(fld.DataType[1]),'');
        continue;
      end;
    end;
    value := '';
    PopIndex:=PopAtField(fld);
    if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then begin
      ov := dbcall.storedata.GetFieldValue(fld.FieldName, rowno);
      RefreshPopUp(PopIndex, fd, rowno);
      value := DbCall.Validate.RefreshField(fld, rowno,true);
      ASBDataObj.FieldToJSONNew(fld, value);
    end else
    begin
      value := DbCall.Validate.RefreshField(fld, rowno,true);
      ASBDataObj.FieldToJSONNew(fld, value);
    end;
    val := dbm.gf.GetNthString(value,1,'~');
    if (value <>'') and (fld.PopIndex > -1) and (fld.IsParentField) and (val = '')  then
      dbcall.Validate.DeleteDetailRows(fd.FrameNo,Rowno);
    if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then begin
     dbcall.validate.UpdatePopParentChanges(fld,val,ov,rowno);
     RefreshPopUp(PopIndex, fd, rowno);
   end;
  end;
end;

function TASBTStructObj.LoadDCCombosNew(xml,sxml,formload_fldlist:string):string;
var dcname,x,s,fldlist,fglist,f ,dcno,loadflag: String;
    fno : integer;
    stime:TDateTime;
    nofill,dcload : boolean;
    i,j : integer;
begin
  result := '';
  servicename:='Quick Load DC Combo';
  try

    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);

    stime:=now;
    dbm := nil;
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in LoadDCCombos') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice LoadDCCombos');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice LoadDCCombos');
    if vartype(xmldoc.DocumentElement.Attributes['dcname']) = varnull then
      raise Exception.create('dcname tag not specified in call to webservice LoadDCCombos');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    nofill := false;
    if vartype(xmldoc.DocumentElement.Attributes['nofill']) <> varnull then
    begin
       if vartostr(xmldoc.DocumentElement.Attributes['nofill']) = 'T' then
          nofill := true;
    end;
    //----
    dcload:=false;
    dcname := xmldoc.DocumentElement.Attributes['dcname'];
    if pos(dcname,fglist) <= 0 then
    begin
      if formload_fldlist <> '' then
      begin
        i := 1;
        fldlist := '';
        while true do
        begin
          f := GetnthString(formload_fldlist,i);
          if f = '' then break;
          j := pos('`',f);
          dcno := copy(f,1,j-1);
          f := copy(f,j+1,length(f));
          if dcname = dcno then
          begin
            dcload := true;
            fldlist := fldlist + f + ',';
          end;
          inc(i);
        end;
      end;
    end else dcload := true;
    if not dcload then raise Exception.create('No autofill/autofield for quick dc load...');
    //----
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick LoadDCCombos webservice');
    dbm.gf.dodebug.msg('---------------------------------------');
    dbm.gf.dodebug.msg('Received XMl ' + xml);
    //dbm.gf.dodebug.msg('Received SXMl ' + sxml);
    x := ASBCommonObj.ValidateSession;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
         ASBDataObj.VisibleDCs := VisibleDCs;
      end;

      x:= xmldoc.DocumentElement.Attributes['transid'];
//      if pos(dcname,fglist) > 0 then
         ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,visibleDCs,fglist,dcname,StructXml);
//      else ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,visibleDCs,fglist,dcname,StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.SubmitClientValuesToSD_New;
      dbCall.struct.visibleDCs := VisibleDCs;
      ASBDataObj.CreateActionObj(x,'tstructs');
      s := dcname;
      delete(s,1,2);
      if s <> '' then fno := strtoint(s);
      dbm.gf.dodebug.msg('Time taken to connect and submit - '+inttostr(millisecondsbetween(now, stime)));
      LoadDCJsonNew(fno,nofill);
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
      dbm.gf.dodebug.msg('Time taken by Quick LoadDCJSON - '+inttostr(millisecondsbetween(now, stime)));
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(ASBCommonObj) then ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      if assigned(dbm) then dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  if assigned(dbm) then
  begin
    dbm.gf.dodebug.msg('Executing Quick LoadDCCombos webservice over');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('');
  end;
  x := CloseProject;
  if result = '' then  result := x
  else if x <> '' then result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

procedure TASBTStructObj.LoadDCJsonNew(FrameNo:Integer;nofill : boolean);
var i , j, k, RCount : integer;
    dcn,v : String;
    frec:pFieldRec;
    fg:pFg;
begin
  dbm.gf.dodebug.msg('Quick Load DC JSON');
  fg:=nil;
  asbDataObj.JSONStr:='';
  if  (dbcall.StoreData.LastSavedRecordId=0) and (not nofill) then
    fg:=ExecFillGrid(Frameno, fgFromLoadDC,'');
//  asbDataObj.DCToJSON(FrameNo,RCount);
  if (assigned(fg)) then
  begin
    RCount := dbcall.StoreData.GetRowCount(FrameNo);
    if RCount > 0  then
    begin
      AsbDataObj.GridToJSON(FrameNo, false) ;
      AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
      PopGridJSON(FrameNo,RCount);
    end;
  end else
    AsbDataObj.LoadDCToJSONNew(FrameNo);
  asbDataObj.EndDataJSON;
  dbm.gf.dodebug.msg('Load DC JSON Completed');
end;

function TASBTStructObj.AddGridRowValuesNew(xml,sxml,formload_fldlist:string):string;
var fldname,x,dc,loadflag,fldlist,fglist  :String;
    fno,rno,i,j : integer;
    f, dcno , dcname : ansistring;
begin
  result := '';
  servicename:='Quick Add Grid Row Values';
  try
    dbm := nil;
    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in AddGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice AddGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice AddGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice AddGridRowValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    {if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);}
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick AddGridRowValues webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XMl ' + xml);
      dbm.gf.dodebug.msg('Received XMl ' + sxml);
      fno:=0;rno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
      begin
         dc := vartostr(xmldoc.DocumentElement.Attributes['dcname']);
         fno := strtoint(dc);
      end;
      if vartostr(xmldoc.DocumentElement.Attributes['rowno']) <> '' then
         rno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['rowno']));
      dcname := 'dc'+dc;

      if formload_fldlist <> '' then
      begin
        i := 1;
        fldlist := '';
        while true do
        begin
          f := GetnthString(formload_fldlist,i);
          if f = '' then break;
          j := pos('`',f);
          dcno := copy(f,1,j-1);
          f := copy(f,j+1,length(f));
          if dcname = dcno then
          begin
            fldlist := fldlist + f + ',';
          end;
          inc(i);
        end;
      end;

      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,visibleDCs,dcname+'`'+dcname,'dc'+dc,StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      dbcall.struct.MakeProperStartIndex;
      ASBDataObj.SubmitClientValuesToSD_New;

      AddGridRowNew(fno, rno);
      Result := ASBDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message+' '+inttostr(rno),false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

procedure TASBTStructObj.AddGridRowNew(FrameNo, RowNo:Integer);
var fm:pFrm;
    i:integer;
    fld:pFld;
begin
   dbCall.Parser.RegisterVar('ActiveRow', 'n', inttostr(rowno));
   Fm:=dbcall.struct.GetFrame(frameno);
   Dbcall.validate.FormLoadPrepareField := true;
   dbCall.StoreData.depCall := true;
   fm.HasDataRows := true;
   asbDataObj.DCToJSON(FrameNo,rowno,rowno);
   For i:=fm.startindex to fm.startindex+fm.FieldCount-1 do begin
     fld:=pFld(dbcall.Struct.flds[i]);
     asbDataObj.FieldToJSONNew(fld, dbcall.Validate.Prepare_Selected_Field(fld,'', RowNo));
   End;
   DbCall.RefreshGridDependents(fm);
   AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
   asbDataObj.EndDataJSON;
end;

function TASBTStructObj.DeleteGridRowValuesNew(xml,sxml,formload_fldlist:string):string;
var fldname,x,activerowdep,s,dc,loadflag,fldlist,fglist,dcname,dcno,f :String;
    fno,rno,k,i,j :integer;
begin
  result := '';
  servicename:='Quick Delete GridRow Values';
  try
    dbm := nil;
    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DeleteGridRowValues') ;
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DeleteGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
      raise Exception.create('recordid tag not specified in call to webservice DeleteGridRowValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DeleteGridRowValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick DeleteGridRowValues webservice');
    x := ASBCommonObj.ValidateSession ;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XMl ' + xml);
      fno:=0;rno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
      begin
         dc := vartostr(xmldoc.DocumentElement.Attributes['frameno']);
         fno := strtoint(dc);
      end;
      if vartostr(xmldoc.DocumentElement.Attributes['rowno']) <> '' then
         rno := strtoint(vartostr(xmldoc.DocumentElement.Attributes['rowno']));

      dcname := 'dc'+dc;
      if formload_fldlist <> '' then
      begin
        i := 1;
        fldlist := '';
        while true do
        begin
          f := GetnthString(formload_fldlist,i);
          if f = '' then break;
          j := pos('`',f);
          dcno := copy(f,1,j-1);
          f := copy(f,j+1,length(f));
          if dcname = dcno then
          begin
            fldlist := fldlist + f + ',';
          end;
          inc(i);
        end;
      end;

      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,visibleDCs,dcname+'`'+dcname,'dc'+dc,StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.CreateActionObj(x,'tstructs');
      dbcall.struct.MakeProperStartIndex;
      ASBDataObj.SubmitClientValuesToSD_New;

      dbCall.RefreshGridDependents(dbcall.Struct.GetFrame(fno));

      AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
      AsbDataObj.EndDataJSON;
      Result:=AsbDataObj.GetJSON;
      dbm.gf.dodebug.msg('Result : ' + result);
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  CloseProject;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.DoFillGridValuesNew(xml,sxml,formload_fldlist:string):string;
var fldname,s,x,v,fgname,loadflag,fglist,dcno,fldlist,f,dcno1,nrows:String;
    enode,n : ixmlnode;
    fno,i,j,rcount,prowcount,k,noofrows:integer;
    fg : pfg;
begin
  result := '';
  servicename:='Quick Do FillGrid';
  try
    dbm := nil;
    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick dc load Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['frameno']) = varnull then
      raise Exception.create('Target DC tag not specified in call to webservice GetFillGridValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick DoFillGridValues webservice');
    x := ASBCommonObj.ValidateSession ;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XML ' + xml);
      fno:=0;
      if vartype(xmldoc.DocumentElement.Attributes['fgname']) <> varnull then
         fgname := vartostr(xmldoc.DocumentElement.Attributes['fgname']);
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
      begin
         dcno := vartostr(xmldoc.DocumentElement.Attributes['frameno']);
         dcno1 := 'dc' + vartostr(xmldoc.DocumentElement.Attributes['frameno']);
         fno := strtoint(dcno);
      end;
      noofrows := 0;
      if vartype(xmldoc.DocumentElement.Attributes['noofrows']) <> varnull then
      begin
         nrows := vartostr(xmldoc.DocumentElement.Attributes['noofrows']);
         dbm.gf.dodebug.msg('Received existing rows ' + nrows);
         if nrows <> ''  then noofrows := strtoint(nrows);
      end;
      if pos(dcno1,fglist) <= 0 then
      begin
        fldlist := '';
        if formload_fldlist <> '' then
        begin
          i := 1;
          fldlist := '';
          while true do
          begin
            f := GetnthString(formload_fldlist,i);
            if f = '' then break;
            j := pos('`',f);
            dcno := copy(f,1,j-1);
            f := copy(f,j+1,length(f));
            if strtoint(copy(dcno,3,10)) > fno then
            begin
              fldlist := fldlist + ',' + f;
            end;
            inc(i);
          end;
        end;
        delete(fldlist,1,1);
      end;
      fglist := dcno1 + '`' +fgname;
      dbm.gf.dodebug.msg('Creating DBCall');
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall('dcload',x,fldlist,'',fglist,dcno1,StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.MakeMemVarNode := true;
      ASBDataObj.SubmitClientValuesToSD_New;
      ASBDataObj.CreateActionObj(x,'tstructs');
      dbcall.struct.MakeProperStartIndex;

      dbm.gf.dodebug.msg('Fillgrid Name : ' + fgname);
      xnode := xmldoc.DocumentElement;
      enode := dbcall.struct.XML.DocumentElement;
      fg := pfg(dbcall.struct.fgs[0]);
      xnode := xnode.ChildNodes.FindNode('GridList');
      if xnode.ChildNodes.Count > 0 then
      begin
        if assigned(fg) then
        begin
          dbm.gf.dodebug.msg('fillgrid assigned');
          if fg.AddRows <> 3 then
          begin
            if fg.AddRows = 2 then dbcall.InitGrid(fg.TargetFrame)
            else begin
              if fg.firmbind then dbcall.InitGrid(fg.TargetFrame)
              else begin
                j := dbCall.StoreData.GetRowCount(fg.TargetFrame);
                dbm.gf.DoDebug.msg('Row count : ' + inttostr(j));
                if (j = 1) and (dbcall.Validate.GetRowValidity(dbCall.StoreData.GetRowCount(fg.TargetFrame),1) = 0) then exit
                else if (j = 1) then dbcall.InitGrid(fg.TargetFrame);
              end;
            end;
          end else AsbDataObj.existing_gridrows := noofrows;
          dbCall.DoMultiSelectFillGrid(fg,xnode);
          dbm.gf.dodebug.msg('Multiselect Row values submitted and depedents refreshed');
          prowcount := dbcall.StoreData.GetRowCount(fno);
          if prowcount > 0 then
             dbcall.Struct.GetFrame(fno).HasDataRows := True;
          AsbDataObj.GridToJSONNew(fno,true);
          AsbDataObj.depCall := true;
          AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
          PopGridJSON(fno,prowcount);
          AsbDataObj.EndDataJSON;
          Result:=AsbDataObj.GetJSON;
          dbm.gf.dodebug.msg('Result : ' + result);
        end;
      end else begin
        AsbDataObj.jsonstr:='';
        ExecFillGrid(fno, fgFromButtonClick,fgname);
        prowcount := dbcall.StoreData.RowCount(fno);
        if prowcount > 0 then
           dbcall.Struct.GetFrame(fno).HasDataRows := True;
        AsbDataObj.GridToJSONNew(fno, true);
        if dbcall.Struct.GetFrame(fno).jsonstr = '' then AsbDataObj.DCToJSON(fno,1);  // as per the requirement for .NET
        AsbDataObj.depCall := true;
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
        PopGridJSON(fno,prowcount);
        AsbDataObj.EndDataJSON;
        Result:=AsbDataObj.GetJSON;
        dbm.gf.dodebug.msg('Result : ' + result);
      end;
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.DoExcelImportValues(xml,sxml:string):string;
var fldname,s,x,v,loadflag,fglist,dcno,fldlist,f,dcno1,formload_fldlist:String;
    enode,n : ixmlnode;
    fno,i,j,rcount,prowcount,k:integer;
begin
  result := '';
  servicename:='Quick Do DoExcelImport';
  if sxml <> '' then
  begin
    StructXml := LoadXMLDataFromWS(sxml);
    formload_fldlist := vartostr(StructXml.DocumentElement.ChildNodes[0].Attributes['wsflds']);
  end;
  try
    dbm := nil;
    loadflag := '';
    i := pos('~',formload_fldlist);
    loadflag := copy(formload_fldlist,1,i-1);
    if loadflag = '' then raise Exception.create('Quick DoExcelImportValues Structure XML not valid...');
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fldlist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    i := pos('~',formload_fldlist);
    fglist := copy(formload_fldlist,1,i-1);
    delete(formload_fldlist,1,i);
    xmldoc := LoadXMLDataFromWS(xml);
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid tag not specified in call to webservice DoFillGridValues');
    if vartype(xmldoc.DocumentElement.Attributes['frameno']) = varnull then
      raise Exception.create('Target DC tag not specified in call to webservice GetFillGridValues');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick DoFillGridValues webservice');
    x := ASBCommonObj.ValidateSession ;
    ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
      dbm.gf.dodebug.msg('Received XML ' + xml);
      fno:=0;
      if vartostr(xmldoc.DocumentElement.Attributes['frameno']) <> '' then
      begin
         dcno := vartostr(xmldoc.DocumentElement.Attributes['frameno']);
         dcno1 := 'dc' + vartostr(xmldoc.DocumentElement.Attributes['frameno']);
         fno := strtoint(dcno);
      end;
      if pos(dcno1,fglist) <= 0 then
      begin
        fldlist := '';
        if formload_fldlist <> '' then
        begin
          i := 1;
          fldlist := '';
          while true do
          begin
            f := GetnthString(formload_fldlist,i);
            if f = '' then break;
            j := pos('`',f);
            dcno := copy(f,1,j-1);
            f := copy(f,j+1,length(f));
            if strtoint(copy(dcno,3,10)) > fno then
            begin
              fldlist := fldlist + ',' + f;
            end;
            inc(i);
          end;
        end;
        delete(fldlist,1,1);
      end;
//      fglist := dcno1 + '`' +fgname;
      dbm.gf.dodebug.msg('Creating DBCall');
      x:= xmldoc.DocumentElement.Attributes['transid'];
      ASBDataObj.CreateAndSetDbCall('dcxlload',x,fldlist,'',fglist,inttostr(fno),StructXml);
      dbCall := ASBDataObj.DbCall;
      ASBDataObj.MakeMemVarNode := true;
      ASBDataObj.SubmitClientValuesToSD_New;
      ASBDataObj.CreateActionObj(x,'tstructs');
      dbcall.struct.MakeProperStartIndex;

//      dbm.gf.dodebug.msg('Fillgrid Name : ' + fgname);
      xnode := xmldoc.DocumentElement;
      enode := dbcall.struct.XML.DocumentElement;
//      fg := pfg(dbcall.struct.fgs[0]);
      xnode := xnode.ChildNodes.FindNode('GridList');
      if xnode.ChildNodes.Count > 0 then
      begin
        dbCall.ExcelGridImport(fno,xnode);
        dbm.gf.dodebug.msg('Multiselect Row values submitted and depedents refreshed');
        prowcount := dbcall.StoreData.GetRowCount(fno);
        if prowcount > 0 then
           dbcall.Struct.GetFrame(fno).HasDataRows := True;
        AsbDataObj.GridToJSONNew(fno, true);
        AsbDataObj.depCall := true;
        AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields);
        PopGridJSON(fno,prowcount);
        AsbDataObj.EndDataJSON;
        Result:=AsbDataObj.GetJSON;
        dbm.gf.dodebug.msg('Result : ' + result);
      end;
    end;
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  x := CloseProject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

function TASBTStructObj.LoadDataNew(s,sxml,nonsaveflds: String): string;
var x , formLoadDBTransids , config_data , axvars_cached , configdata_cached , transid : String;
    f:extended;
    datavalidate : boolean;
    i : integer;
begin
  result := '';
  servicename:='Quick Load Data';
try
  xmldoc := LoadXMLDataFromWS(s);
  if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
    raise Exception.create('Sessionid not specified in LoadData');
  if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
    raise Exception.create('axpapp tag not specified in to loaddata webservice');
  if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
    raise Exception.create('transid tag not specified in parameter');
  if vartype(xmldoc.DocumentElement.Attributes['recordid']) = varnull then
    raise exception.create('recordid attribute not specified');
  if xmldoc.DocumentElement.HasAttribute('trace') then
     tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
  if xmldoc.DocumentElement.HasAttribute('scriptpath') then
     spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
  x := xmldoc.documentelement.attributes['axpapp'];
  openConnect := False;
  ConnectToProject(x);
  dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
  dbm.gf.dodebug.msg('Executing Quick LoadData webservice');
  dbm.gf.dodebug.msg('-----------------------------------');
  dbm.gf.dodebug.msg('Received XML : ' + s);
  x := ASBCommonObj.ValidateSession;
  ASBDataObj.ChangeDCCap := True;//Dynamic Dc Caption
  if copy(x,1,7) = '<error>' then
     result := CreateErrNode(x,false)
  else
  begin
    datavalidate := false;
    {
    if vartype(xmldoc.DocumentElement.Attributes['dataval']) <> varnull then
      if vartostr(xmldoc.DocumentElement.Attributes['dataval']) <> '' then
       datavalidate := strtobool(vartostr(xmldoc.DocumentElement.Attributes['dataval']));
      }
    if vartype(xmldoc.DocumentElement.Attributes['imagefromdb']) <> varnull then
      if vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']) <> '' then
       ASBDataObj.imagefromdb := strtobool(vartostr(xmldoc.DocumentElement.Attributes['imagefromdb']));
    if vartostr(xmldoc.DocumentElement.Attributes['dcname']) <> '' then
    begin
       VisibleDCs := GetQoutedVisibleDCNames(vartostr(xmldoc.DocumentElement.Attributes['dcname']));
       ASBDataObj.VisibleDCs := VisibleDCs;
    end;
    if vartype(xmldoc.DocumentElement.Attributes['axvars_cached']) <> varnull then
       axvars_cached := vartostr(xmldoc.DocumentElement.Attributes['axvars_cached']);
    if vartype(xmldoc.DocumentElement.Attributes['configdata_cached']) <> varnull then
       configdata_cached := vartostr(xmldoc.DocumentElement.Attributes['configdata_cached']);

    x:= xmldoc.DocumentElement.Attributes['transid'];
    transid := x;
//    ASBDataObj.CreateAndSetDbCall('',x,'',visibleDCs,'','',StructXml);
    ASBDataObj.CreateAndSetDbCall(x,sxml);
    ASBDataObj.DbCall.dataval := datavalidate;
    dbCall := ASBDataObj.DbCall;
    ASBDataObj.DataSubmitSort := false;
    if sxml = '' then tcstr := ASBDataObj.CheckForAccess(x);
    ASBDataObj.CreateActionObj(x,'tstructs');
    ASBDataObj.SubmitClientValuesToSD_New;
    CreateNonSaveFlds(nonsaveflds);
    dbCall.struct.visibleDCs := visibleDCs;

    ASBCommonObj.structrecord := vartostr(xmldoc.DocumentElement.attributes['recordid']);
    x := xmldoc.DocumentElement.attributes['recordid'];
    dbm.gf.doDebug.Msg('Record id : ' + x);
    f := dbm.gf.strtofloatz(x);
    if f>0 then
    begin
      if Assigned(dbcall.StoreData) then
      begin
        //if the call is from peg editable form , then do not init peg / amendment flow
        if vartype(xmldoc.DocumentElement.Attributes['ispegedit']) <> varnull then
        begin
           if lowercase(vartostr(xmldoc.DocumentElement.Attributes['ispegedit'])) = 'true' then
           begin
            dbcall.StoreData.IsPegEdit := true;
           end;
        end;
        dbcall.StoreData.SDEvaluateExpr := SDEvaluateExpr;
        dbcall.StoreData.SDRegVarToParser := SDRegVarToParser;
        dbcall.StoreData.Object_ASBDataObj := ASBDataObj;
        dbcall.StoreData.ParserObject := dbcall.Parser;
      end;
      dbcall.LoadDataForWeb(f);
      if (tcstr <> 'n') then
      begin
//        if dbcall.StoreData.empty_load_flds  <> '' then
//           nonsaveflds := nonsaveflds + dbcall.StoreData.empty_load_flds ;
        PrepareNonSaveFlds(nonsaveflds);
        FindAndExeDataLoadAction;
        dbCall.struct.MakeProperStartIndex;
//        dbcall.Struct.flds.sort(sortfldsDefault);
        LoadDataJSONNew;
        Result := ASBDataObj.GetJSON;
        //----
        if axvars_cached = 'f' then
        begin
          formLoadDBTransids := dbCall.Parser.GetVarValue('forms_transids');
          if formLoadDBTransids <> '' then
          begin
             if pos(','+transid+',',formLoadDBTransids) > 0 then
                ASBCommonObj.GetDBVarsForFormsAndReports(transid,'tstruct',dbcall.Parser);
          end;
        end;
        config_data := '';
        if configdata_cached = 'f' then
        begin
          formLoadDBTransids := dbCall.Parser.GetVarValue('configparam_transids');
          if formLoadDBTransids <> '' then
          begin
             if pos(','+transid+',',formLoadDBTransids) > 0 then
                config_data := ASBCommonObj.GetConfigParamForFormsAndReports(transid,dbcall.Parser);
          end;
        end;
        //----
        if result <> '' then
        begin
          if dbm.gf.AxMemVars <> '' then result := result + dbm.gf.AxMemVars;
          if config_data <> '' then result := result + '$*$' + config_data;
        end;
        dbm.gf.dodebug.msg('Result : ' + Result);
      end;
    end;
  end;
  except
    on E:Exception do
    begin
      GenerateSessionAppKey := False;
      ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      dbm.gf.dodebug.msg('Error : ' + e.Message);
      if assigned(dbm.Connection) then
      begin
        dbm.gf.execActName := 'LoadData';
        if assigned(dbcall) then dbm.update_errorlog(dbcall.transid,dbcall.ErrorStr);
        dbm.gf.execActName := '';
      end;
    end;
  end;
  dbm.gf.dodebug.msg('Quick LoadData webservice completed');
  dbm.gf.dodebug.msg('');
  dbm.gf.dodebug.msg('');
  x := closeproject;
  if result = '' then  result := x
  else result := x + '*$*' +  result;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

procedure TASBTStructObj.CreateNonSaveFlds(nonsaveflds : string);
  var i : integer;
      f,dcno : string;
      fld : pFld ;
begin
  i := 1;
  while true do
  begin
    f := GetnthString(nonsaveflds,i);
    if f = '' then break;
    fld := dbCall.struct.GetField(f);
    if assigned(fld) then
    begin
      if assigned(dbcall.StoreData.NoSaveFldDcs) then
      begin
        dcno := inttostr(fld.FrameNo);
        if dbcall.StoreData.NoSaveFldDcs.IndexOf(dcno) = -1 then
           dbcall.StoreData.NoSaveFldDcs.Add(dcno);
      end else
      begin
       dbcall.StoreData.NoSaveFldDcs := TStringList.Create;
       dbcall.StoreData.NoSaveFldDcs.Add(inttostr(fld.FrameNo));
      end;
    end;
    inc(i);
  end;
end;

procedure TASBTStructObj.PrepareNonSaveFlds(nonsaveflds : string);
  var i,k,j,r : integer;
      fd : pFld;
      fm : pFrm;
      f, fname, fvalue , refreshonload : String;
begin
//  nonsaveflds := dbCall.CreateFldListByOrder(nonsaveflds);
  refreshonload := dbcall.Parser.GetVarValue('Axp_RefreshExpsOnLoad');
  for i := 1 to dbCall.struct.frames.Count do
  begin
    if assigned(dbcall.StoreData.NoSaveFldDcs) and (dbcall.StoreData.NoSaveFldDcs.IndexOf(inttostr(i)) = -1) then continue;
    k := dbCall.StoreData.GetRowCount(i);
    if k = 0 then continue;
    fm:=pFrm(dbCall.struct.Frames[i-1]);
    for r := 1 to k do begin
      if fm.Popup then
        dbCall.Validate.GetParentActiveRow(r,fm.popindex)
      else
        dbCall.Validate.Parser.Registervar('activeprow', Char('n'), inttostr(r));
      dbCall.Validate.RegRow(i , r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(dbCall.struct.flds[j]);
        fname := fd.fieldname;
        fvalue := '';
        if lowercase(copy(fname, 1, 3)) = 'old' then
        begin
          fvalue := dbcall.StoreData.GetFieldValue(copy(fname, 4, length(fname)), r);
          dbm.gf.DoDebug.msg(fname+ ' row = '+inttostr(r)+' Value = '+fvalue);
          dbcall.StoreData.SetFieldValue(fname,fd.datatype,fd.tablename,fvalue,fvalue,r,0,0,fd.frameno,0,fd.sourcekey);
          dbCall.Validate.Parser.RegisterVar(fname,fd.DataType[1],fvalue);
        end;
        if (not fd.SaveValue) then dbCall.Validate.Prepare_Selected_Field(fd,r,fvalue)
        else if (fd.ModeofEntry = 'calculate') and (fd.cexp <> '') then
        begin
          if lowercase(refreshonload) <> 'false' then
          begin
            fvalue := dbcall.StoreData.GetFieldValue(copy(fname, 4, length(fname)), r);
            if ((fd.DataType = 'n') and (isnumeric(fvalue)) and(dbm.gf.StrToFloatz(fvalue) <> 0)) or (fvalue = '')  then
            begin
              If dbCall.Validate.Parser.EvalPrepared(fd.Exprn) Then begin
                fvalue := dbCall.Validate.Parser.Value;
                if (fd.DataType = 'n') and (fvalue <> '') then fvalue := floattostr(dbm.gf.RealRound(strtofloat(fvalue),fd.Dec));
                if fd.DataType = 'n' then fvalue := dbCall.Validate.FormatNumber(fd,fvalue);
                dbCall.StoreData.SubmitValue(fd.FieldName, r, fvalue, '', 0, 0, 0);
                dbCall.Validate.Parser.RegisterVar(fd.FieldName,fd.DataType[1],fvalue);
              end;
            end;
          end;
        end;
      end;
    end;
  end;
  {
  i := 1;
  priorfrmno := 0;
  while true do
  begin
    f := GetnthString(nonsaveflds,i);
    if f = '' then break;
    fld := dbCall.struct.GetField(f);
    if assigned(fld) then
    begin
      if priorfrmno <> fld.FrameNo then
      begin
        k := dbCall.StoreData.RowCount(fld.FrameNo);
        priorfrmno := fld.FrameNo
      end;
      if k = 0 then
      begin
        inc(i);
        continue;
      end;
      fm:=pFrm(dbCall.struct.Frames[fld.FrameNo-1]);
      fname := fld.fieldname;
      for r := 1 to k do begin
        FieldValue := '';
        if lowercase(copy(fname, 1, 3)) = 'old' then
        begin
          FieldValue := dbcall.StoreData.GetFieldValue(copy(fname, 4, length(fname)), r);
          dbm.gf.DoDebug.msg(fname+ ' row = '+inttostr(r)+' Value = '+FieldValue);
          dbcall.StoreData.SetFieldValue(fname,fld.datatype,fld.tablename,FieldValue,FieldValue,r,0,0,fld.frameno,0,fld.sourcekey);
        end;
        if fm.Popup then
          dbCall.Validate.GetParentActiveRow(r,fm.popindex)
        else
          dbCall.Validate.Parser.Registervar('activeprow', Char('n'), inttostr(r));
        dbCall.Validate.RegRow(fld.FrameNo , r);
        dbCall.Validate.Prepare_Selected_Field(fld,r,FieldValue);
      end;
    end;
    inc(i);
  end;
  }
end;

procedure TASBTStructObj.LoadDataJSONNew;
var i , j, k, RCount, PriorRow, PriorFrame, r  : integer;
    dcn,v : String;
    frec:pFieldRec;
    WithDropDown: Boolean;
    fm:pFrm;

    bIsAmendTrans : Boolean;
    sAmendmentStatus,sPEGStatus,sDataJson : String;
begin
  dbm.gf.dodebug.msg('Quick Load Data JSON');
  asbDataObj.JSONStr:='';

  for j := 0 to dbcall.StoreData.structdef.frames.Count-1 do begin
     ASBDataObj.DCToJSON(pFrm(dbcall.StoreData.structdef.frames[j]), -1);
  end;

  priorframe:=0;PriorRow:=0;
  withdropdown:=false;
  i:=0;
  While (i<dbcall.struct.flds.Count) do begin
    fld := pfld(dbcall.struct.flds[i]);

    fm:=dbcall.Struct.GetFrame(fld.FrameNo);

    if (fm.Popup) or (fm.FieldCount=0) then
    begin
      inc(i);
      continue;
    end;

    if priorframe <> fld.frameno then begin

      RCount := dbcall.StoreData.RowCountForWeb(fld.frameno);
      if RCount = 0  then
      begin
         r:=1;
         AsbDataObj.DummyDCNodeToJSON(fld.FrameNo,r,'i1');  // as per the requirement for .NET
         j := fm.StartIndex+fm.FieldCount;
        if j < i then
          inc(i)
        else i := j ;
        continue;
      end;
      PriorFrame := fld.FrameNo;
      PriorRow:=0;
      WithDropDown := false;
      if fld.AsGrid then
      begin
        j := asbDataObj.GridToJSONNew(fld.FrameNo, WithDropDown);
        if RCount > 0  then PopGridJSON(fld.FrameNo,rcount);
        if j < i then
          inc(i)
        else i := j ;
        continue;
      end;
    end;

    if fld.datarows.count>0 then begin
      for j := 0 to fld.datarows.count-1 do begin
        k:=strtoint(fld.datarows[j]);
        if pFieldRec(dbcall.storedata.fieldlist[k]).RowNo<0 then continue;
        if ((fld.ModeOfEntry='select') and (not fld.FromList) and (fld.ComponentType<>'')) or ((dbm.gf.AutoselectCompatibility) and (fld.AutoSelect)) then
            Dbcall.validate.Prepare_Selected_Field(fld,'loaddata') ;
        asbDataObj.LoadDataFieldToJSON(fld, pFieldRec(dbcall.storedata.fieldlist[k]));
      end;
    end else begin
      frec:=nil;
      frec:= pFieldRec(dbcall.storedata.getfieldrec(fld.fieldname, 1));
      if assigned(frec) then
        asbDataObj.LoadDataFieldToJSON(fld, frec)
      else if fld.DataType = 'i' then begin
        if fld.Tablename <> '' then
          asbDataObj.ImageFieldToJSON(fld)
        else begin
          If (fld.Exprn >= 0) Then Begin
            If Dbcall.Parser.EvalPrepared(fld.Exprn) Then
            begin
              v := Dbcall.Parser.Value;
              if v <> '' then
                asbDataObj.ImageFieldToJSON(fld,v)
            end;
          end;
        end;
      end;
    end;
    inc(i);
  end;
  asbDataObj.EndDataJSON;

  //Add PEG | Amend along with loaddata response if exists
  if (dbcall.StoreData.structdef.isPegAttached or dbcall.StoreData.structdef.IsAmendmentEnabled)
     and (AnsiStartsStr('{"data":[',asbDataObj.jsonstr)) then
  begin
    sAmendmentStatus := dbm.gf.GetNthString(dbm.gf.sAxPegStatus,1,'$#*#$'); // read Amendment status
    sPEGStatus := dbm.gf.GetNthString(dbm.gf.sAxPegStatus,2,'$#*#$'); //Read PEG status

    bIsAmendTrans := CompareText(dbm.gf.GetNthString(sAmendmentStatus,1,'$##$'),'true') = 0;
    if bIsAmendTrans {dbcall.StoreData.structdef.IsAmendmentEnabled} then //if Amend trans
    begin
      sDataJson := dbm.gf.GetNthString(sAmendmentStatus,4,'$##$');
      if sDataJson = '' then
        sDataJson := '{}';
      asbDataObj.jsonstr := asbDataObj.jsonstr +'*$*'+'{"axamend":[{"readonlytrans":"'+ifthen(dbm.gf.bAxPegReadOnlyTrans,'true','false')+'"'+
                            ',"status":"'+dbm.gf.GetNthString(sAmendmentStatus,2,'$##$')+'"'+  //status
                            ',"enableactions":"'+dbm.gf.GetNthString(sAmendmentStatus,3,'$##$')+'"'+  //enable peg action buttons
                            //data we need as in JSON object format so removed string quotes for values
                            ',"data":'+sDataJson+''+  //amend data
                            ',"withdraw":"'+dbm.gf.GetNthString(sAmendmentStatus,5,'$##$')+'"'+  //withdraw
                            ',"confirmmsg":"'+dbm.gf.GetNthString(sAmendmentStatus,6,'$##$')+'"'+  //Amend confirm msg
                            ',"displaymsg":"'+dbm.gf.GetNthString(sAmendmentStatus,7,'$##$')+'"'+  //AmendStatus for Display purpose
                            ',"comments":"'+dbm.gf.GetNthString(sAmendmentStatus,8,'$##$')+'"'+  //Approver comments
                            ',"recall":"'+dbm.gf.GetNthString(sAmendmentStatus,9,'$##$')+'"'+  //recall
                            '}]}';
    end;
    //if PEG attached
    if dbcall.StoreData.structdef.isPegAttached then
    begin
    sDataJson := dbm.gf.GetNthString(sPEGStatus,4,'$##$');
    if sDataJson = '' then
      sDataJson := '{}';
    asbDataObj.jsonstr := asbDataObj.jsonstr +'*$*'+'{"axpeg":[{"readonlytrans":"'+ifthen(dbm.gf.bAxPegReadOnlyTrans,'true','false')+'"'+
                          ',"status":"'+dbm.gf.GetNthString(sPEGStatus,2,'$##$')+'"'+  //status
                          //if amendment enabled then disable peg action buttons
                          ',"enableactions":"'+dbm.gf.GetNthString(sPEGStatus,3,'$##$')+'"'+  //enable peg action buttons
                          ',"data":'+sDataJson+''+  //peg data
                          ',"withdraw":"'+dbm.gf.GetNthString(sPEGStatus,5,'$##$')+'"'+  //withdraw
                          ',"confirmmsg":"'+dbm.gf.GetNthString(sPEGStatus,6,'$##$')+'"'+  //Peg confirm msg |not reuqired now
                          ',"displaymsg":"'+dbm.gf.GetNthString(sPEGStatus,7,'$##$')+'"'+  //Peg Status for Display purpose |not reuqired now
                          ',"comments":"'+dbm.gf.GetNthString(sPEGStatus,8,'$##$')+'"'+  //Approver comments
                          ',"recall":"'+dbm.gf.GetNthString(sPEGStatus,9,'$##$')+'"'+  //recall
                          ',"finalapproval":"'+dbm.gf.GetNthString(sPEGStatus,10,'$##$')+'"'+  //Final approval done or not
                          '}]}';
    end;
  end;

  dbm.gf.dodebug.msg('Quick Load Data JSON Completed');
end;

Function TASBTStructObj.GetNthString(SrcString: String; StrPos: integer): String;
Var
  i, k: integer;
Begin
  Result := '';
  i := 1;
  k := 1;
  while (k <= strpos) and (i<=length(srcstring)) do begin
   if srcstring[i] = ',' then
    inc(k)
   else if k = strpos then
    result := result + srcstring[i];
   inc(i);
  end;
end;

function TASBTStructObj.DepFillGridNew(FrameNo:Integer; depflds : TStringList):String;
var i, RCount, fno:integer;
    fg:pFg;
    fm:pFrm;
begin
  dbm.gf.DoDebug.msg('Filling dependent grid '+ IntToStr(FrameNo));
  result:='';
  fg:=ExecFillGrid(FrameNo, fgFromDependents,'');
  if assigned(fg) then begin
    fm:=dbcall.Struct.GetFrame(FrameNo);
    result:=','+inttostr(FrameNo)+',';
    RCount := dbcall.StoreData.RowCount(FrameNo);
    if RCount = 0 then fm.HasDataRows := false;
    AsbDataObj.GridToJSONNew(fg.TargetFrame, true);
    if fm.jsonstr = '' then AsbDataObj.DummyDCNodeToJSON(FrameNo,1,'d*,i1')  // as per the requirement for .NET
    else AsbDataObj.GridDependentsToJSON(dbcall.GridDependentFields,depflds);
    PopGridJSON(FrameNo,RCount);
    for i := 0 to dbcall.struct.popgrids.Count - 1 do begin
      if FrameNo=pPopGrid(dbcall.struct.popgrids[i]).ParentFrameNo then begin
        fno:=pPopGrid(dbcall.struct.popgrids[i]).FrameNo;
        result:=result+','+inttostr(fno)+',';
      end;
    end;
  end;
end;

function TASBTStructObj.NonGridToGridNew(fd,dfd:pFld) : string;
var r, j, rcount, PopIndex:integer;
    fm:pFrm;
    fld:pfld;
    value : String;
begin
  result := '';
  dbm.gf.DoDebug.msg('Refreshing dependents of '+fd.FieldName+' in grid '+IntToStr(dfd.FrameNo));
  fm:=dbcall.Struct.GetFrame(dfd.FrameNo);
  rcount:=dbcall.StoreData.RowCount(dfd.FrameNo);
  if not fm.Popup then
  begin
    if rcount = 0 then
    begin
      fld := dbcall.struct.GetField('validrow'+inttostr(fm.FrameNo));
      if fld <> nil then
      begin
         value := dbcall.validate.RefreshField(fld, 1,true);
         if copy(uppercase(value),1,1) = 'T' then
         begin
           fm.HasDataRows := true;
           rcount := 1;
         end;
      end;
      for r := 1 to rcount do begin
        dbcall.validate.RegRow(dfd.FrameNo, r);
        for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
          fld:=pFld(dbcall.struct.flds[j]);
          if not assigned(fld) then continue;
          if fd.FieldName = fld.FieldName then continue;
          if fd.Dependents.IndexOf(fld.FieldName)=-1 then continue;
          value := '';
          PopIndex:=PopAtField(fld);
          if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
            result := result + RefreshPopUp(PopIndex, fd, r);
          if fd.Dependents.IndexOf(fld.FieldName)>-1 then begin
            value := dbcall.validate.RefreshField(fld, r,true);
           if (value <> '') or((fld.DataType = 'n') and (StrToFloat(value) <> 0)) then
           begin
             fm.HasDataRows := true;
           end;
            ASBDataObj.FieldToJSONNew(fld, value);
          end;
          if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then
            result := result + RefreshPopUp(PopIndex, fd, r);
        end;
      end;
      exit;
    end;
  end;
  for r := 1 to rcount do begin
    dbcall.validate.RegRow(dfd.FrameNo, r);
    for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
      fld:=pFld(dbcall.struct.flds[j]);
      if not assigned(fld) then continue;
      if fd.FieldName = fld.FieldName then continue;
      if fd.Dependents.IndexOf(fld.FieldName)=-1 then continue;
      if (fld.ModeofEntry = 'select') and ((not fld.Autoselect) and (fld.cexp='')) then
      begin
        if (dbcall.StoreData.GetFieldValue(fd.FieldName,1) = '') then
        begin
          ASBDataObj.FieldToJSONNew(fld, '`'+ inttostr(r) +' `0`0');
          dbcall.StoreData.SubmitValue(fld.FieldName, r, '', '', 0, 0, 0);
          dbcall.Parser.RegisterVar(fld.FieldName,Char(fld.DataType[1]),'');
          continue;
        end;
      end;
      value := '';
      PopIndex:=PopAtField(fld);
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField<>'') then
        result := result + RefreshPopUp(PopIndex, fd, r);
      if fd.Dependents.IndexOf(fld.FieldName)>-1 then begin
        value := dbcall.validate.RefreshField(fld, r,true);
        ASBDataObj.FieldToJSONNew(fld, value);
      end;
      if (PopIndex>-1) and (pPopGrid(dbcall.struct.popgrids[PopIndex]).DispField='') then
         result := result + RefreshPopUp(PopIndex, fd, r);
    end;
  end;
end;

function TASBTStructObj.GlobalVarsInSqls  : String;
  var i : integer;
      s,n,v,d : string;
begin
  result := '';
  if not assigned(dbm.gf.GlobalVarsInSqlParams) then exit;
  s := '';
  for i := 0 to dbm.gf.GlobalVarsInSqlParams.Count - 1 do
  begin
    n := trim(dbm.gf.GlobalVarsInSqlParams.Names[i]);
    v := trim(dbm.gf.GlobalVarsInSqlParams.Values[n]);
    v:=dbm.gf.FindAndReplace(v, '"', '^^dq');
    s := s + '{"n":"'+n+'","v":"'+v+'"},';
  end;
  if s <> '' then
  begin
    delete(s,length(s),1);
    result := s;
  end;
end;

function TASBTStructObj.GetAxMemLoadVars(xml : string):string;
var x : String;
    stime:TDateTime;
    parser : TEVal;
begin
  result := '';
  servicename:='GetAxMemLoadVars';
  FastDataFlag := true;
  stime:=now;
  try
    if xml = '' then
      raise Exception.create('Input Data not available to execute GetAxMemLoadVars web service...');
    xmldoc := LoadXMLDataFromWS(xml);
    dbm := nil;
    parser := nil;
    if vartype(xmldoc.DocumentElement.Attributes['sessionid']) = varnull then
      raise Exception.create('Sessionid not specified in GetAxMemLoadVars');
    if vartype(xmldoc.DocumentElement.Attributes['axpapp']) = varnull then
      raise Exception.create('axpapp tag not specified in call to webservice GetAxMemLoadVars');
    if vartype(xmldoc.DocumentElement.Attributes['transid']) = varnull then
      raise Exception.create('transid not specified in call to webservice GetAxMemLoadVars');
    if xmldoc.DocumentElement.HasAttribute('trace') then
       tfile := vartostr(xmldoc.DocumentElement.Attributes['trace']);
    if xmldoc.DocumentElement.HasAttribute('scriptpath') then
       spath := vartostr(xmldoc.DocumentElement.Attributes['scriptpath']);
    x := xmldoc.documentelement.attributes['axpapp'];
    openConnect := false;
    ConnectToProject(x);
    dbm.gf.sessionid := trim(xmldoc.DocumentElement.Attributes['sessionid']);
    dbm.gf.dodebug.msg('Executing Quick GetAxMemLoadVars webservice');
    dbm.gf.dodebug.msg('-------------------------------------');
    x := ASBCommonObj.ValidateSession;
    if copy(x,1,7) = '<error>' then
       result := CreateErrNode(x,false)
    else
    begin
       dbm.gf.dodebug.msg('Received XML ' + xml);
       x:= xmldoc.DocumentElement.Attributes['transid'];
       parser := TEVal.Create(axprovider);
       parser.axp := axprovider;
       RegisterAppValues(parser);
       ASBCommonObj.GetDBVarsForFormsAndReports(x,'tstruct',Parser);
       if dbm.gf.AxMemVars <> '' then
       begin
         delete(dbm.gf.AxMemVars,1,3);
         result := dbm.gf.AxMemVars;
       end;
    end;
    dbm.gf.dodebug.msg('Result : ' + Result);
    dbm.gf.dodebug.msg('Time taken by GetAxMemLoadVars - '+inttostr(millisecondsbetween(now, stime)));
  except
    on E : Exception do
    begin
      GenerateSessionAppKey := False;
      if assigned(ASBCommonObj) then ASBCommonObj.serviceresult:=copy(e.message,1,199);
      result := CreateErrNode(E.Message,false);
      if assigned(dbm) then dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
  try
    if assigned(parser) then
    begin
        parser.free;
        parser := nil;
    end;
  except
  end;
  if assigned(dbm) then
  begin
    dbm.gf.dodebug.msg('GetAxMemLoadVars webservice completed');
    dbm.gf.dodebug.msg('');
    dbm.gf.dodebug.msg('');
    x := CloseProject;
    if result = '' then  result := x
    else result := x + '*#*' +  result;
  end;
  if close_err <> '' then result:= CreateErrNode(close_err,false)
  else if appsessionkey <> '' then result:= CreateResultWithAppSessionKey(nil,result);
end;

procedure TASBTStructObj.RegisterAppValues(Parser:TEVal);
var j :integer;
    f,v,dt,srecid,s:String;
    n,rnode : ixmlnode;
begin
  dbm.gf.dodebug.msg('Submiting & Registering data ');
  RegisterGlobalVarsforDBFunc(Parser);
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('data'); // his node used for savedata service
  if rnode = nil then rnode := xmldoc.DocumentElement.ChildNodes.FindNode('FieldList'); // This node used for dofillgridvalues service
  if rnode = nil then n := xmldoc.DocumentElement.ChildNodes.FindNode('varlist'); // his node used for action service
  if assigned(n) then rnode := n.ChildNodes[0];
  if rnode = nil then rnode := xmldoc.DocumentElement;
  dbm.gf.dodebug.msg('REgistering Node : ' + rnode.XML);
//  if assigned(rnode) then rnode := rnode.ChildNodes[0];
  try
    if rnode.HasChildNodes then
    begin
      for j:=0 to rnode.childnodes.count-1 do begin
        n := rnode.childnodes[j];
        f:=vartostr(n.NodeName);
        dbm.gf.dodebug.msg('Var Name : ' + f);
        if (f = 'globalvars') or (f = 'uservars') then continue;
        v:=vartostr(n.NodeValue);
        if pos('<br>',v) > 0  then v := dbm.gf.FindAndReplace(v,'<br>',#$D#$A);
        if vartostr(n.Attributes['t']) <> '' then
           dt := vartostr(vartostr(n.Attributes['t']))
        else dt := 'c';
        Parser.RegisterVar(f,dt[1],v);
      end;
    end;
    except on E : Exception do
    begin
      if assigned(dbm) then dbm.gf.dodebug.msg('Error : ' + e.Message);
    end;
  end;
end;


Procedure TASBTStructObj.RegisterGlobalVarsforDBFunc(Parser:TEVal);
var n,rnode : ixmlnode;
    j : integer;
    f,v,dt : String;
begin
  dbm.gf.dodebug.msg('Registering Global variables data ');
  rnode := xmldoc.DocumentElement.ChildNodes.FindNode('globalvars');
  if rnode <> nil then
  begin
    for j:=0 to rnode.childnodes.count-1 do begin
      n := rnode.childnodes[j];
      f:=vartostr(n.NodeName);
      v:=vartostr(n.NodeValue);
      if vartype(n.Attributes['dt']) <> varnull then
         dt := vartostr(n.Attributes['dt'])
      else dt := 'c';
      Parser.RegisterVar(f,dt[1],v);
      dbm.gf.dodebug.msg('Register Global Var : '+ f + '=' +v);
    end;
  end;
  dbm.gf.dodebug.msg('Registering Global variables data over');
end;

end.



