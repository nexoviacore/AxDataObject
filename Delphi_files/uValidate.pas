unit uValidate;
{copied from 11.3\AsbCommonObjs}
interface
Uses db, classes, forms, sysutils, uGeneralFunctions, uStructdef,
Extctrls, controls, Variants, uProfitEval, uStoreData,
uXDS, uDoDebug , uAxProvider,DateUtils,uExecuteSQL,XMLDoc,XMLIntf;

type
TExecuteFillGrid = procedure(frmno:integer) of object;
PCompanySQL = ^TCompanySQL;
TCompanySQL = Record
  SQLText: String;
  AutoSelect : Boolean;
  Refresh : Boolean;
  Silent : Boolean;
End;

TValidate = class
private
 IdValue : extended;
 FieldValue : String;
 WorkQuery, RepExpr, FromList, fCheck : TXDS;
 CompanySQL: pCompanySQL;
 FieldIndex, LastRegisteredRow, FCount : integer;
 fStoreData : TStoreData;
 fSQLText : String;
 fAutoSelect : Boolean;
 FrameNo : Integer;
 fRefreshOnSave, fSilent, onsave, TrimRows  : Boolean;
 FrameNames : String;
 RowList, ParentRowList,ParentList : TStringlist;
 LastIdValue : Extended;
 axp : TAxProvider;
// XQuery : TXDS;
 AutoVal: Boolean;
 isonline : Boolean;

 Procedure PrepareWorkQuery(SQLString: String);
 Procedure FillCompanyParams(pQuery: TXDS; RowNo: integer); overload ;
 Procedure FillCompanyParams(pQuery: TXDS; RowNo: integer ;fieldName,Fvalue : String) ; overload  ;
// procedure FillFieldValue(FieldName:String;rowno:integer);
// Function GetValue(FieldName:String;rowno:integer):String;
 Function GetId(FieldName:String;rowno:integer):extended;
 Procedure GetAutoGenNo(RowNo:Integer);
 Function GetFieldType(fname:String):String;
 function CheckDuplicate(RowNo: integer): Boolean;
 function ValidateData(RowNo:integer): boolean;
 function DuplicateInGrid(RowNo: integer): Boolean;
 function FillCompositeParams(S: String): String;
 function GetAllValues(FName: String): String;
 procedure SetStoreData(Sd:TStoreData);
 function CheckWidth: boolean;
 procedure AddToFromListValues(fname:String);
 procedure FillParentRow;
 function InsertFilterCondition(SqlText,FName, FValue : String): String;
 procedure InsertParentDetails(RowNo: integer);
 procedure GetParentValue(frmno,rowno: integer; pFields: String);overload;
    function GetErrorStrForPopGrid(RowNo: Integer): String;
    procedure GetPopParentValue(frmno, rowno: integer; pFields: String);
    function GetActualRowNo(RowNo, Popindex: Integer): Integer;
    function GetFldPrompt(fld: pFld; RowNo: integer): String;
    function IsParentValueFound(FrameNo, Rowno: integer): Boolean;
    function TaxCompute(fd: pfld;rno:integer): String;
    function MakeProperValue(Str: String): String;
    function ChangeSQL(SqlText,SearchValue:String;SearchFldList:TStringList;SearchIdx:Integer):String;
    procedure fillnonsavefieldvalues;
    procedure CopyQResult(q: TXDS; popgrid: pPopgrid);
    procedure RefreshPopGrid(Frmno: Integer);
    function GetErrorStr(m: String; RowNo: Integer): String;
    procedure RefreshFldDepedents(fd: pFld; rowno: integer);
    procedure GetAutoGenData(RowNo: Integer);
    procedure SetUsedQuotedStrProp(xfld: pfld);
 function ValidateSQL(transid,FName,FValue: String): Boolean;
 function ValidateExpression(transid, FName, FValue: String): Boolean;
    function QueryOpenForPickList(Q: TXDS; RowNo: integer; FName,
      fValue: String): TXDS;


public
 SubTypeField : String;
 ErrorStr : String;
 Validation, Loading,forpicklist,DeleteTrans : Boolean;
 Parser : TProfitEval;
 Query : TXDS;
 FreeParser, AutoAddToList, IsError : Boolean;
 FromMap : Boolean;
 ParentRowno : Integer;
 SubFrameNo     : Integer;
 ParentFrameNo : Integer;
 SubFormCall, ZeroValue : Boolean;
 sdef : TStructdef;
 QueryList : TList;
 DynamicSQL : String;
 ExecuteFillGrid : TExecuteFillGrid;
 ValidRowPos,CurRow,ActualRowNo : Integer;
 fld : pfld;
 ActualRows : TStringlist;
 starttime : TDatetime;
 error_fieldname,error_row,error_value : String; // to send to client side in case of validation error
 InFrame : boolean;
 chgrowdcs,chgrownos : TStringlist;
 OnWebSave : Boolean;
 DeleteRowOnError : Boolean;
 FldDependentFields : String;
 WarningMsg, ivParamList : String;
 FillOnLoadFlds : TStringList;
 FastDataFlag, NonDynamicFilterSQL : Boolean;
 FormLoadPrepareField , dependcall : Boolean;
 FromADR,ForIviewDef : Boolean;
 XQuery : TXDS;

 Constructor Create(AxPro : TAxProvider); Virtual;
 Destructor Destroy; override;
 Function FillAndValidate(TrimEmptyRows:Boolean) : Boolean;
 Function GetRelevantExpression(PExpressionId: Integer): String;
 Procedure QueryOpen(Q:TXDS;RowNo: integer); overload ;
 Procedure QueryOpen(Q:TXDS;RowNo: integer ;fName, fValue  : String); overload;
 procedure RegisterRow;
 procedure RegRow(FrameNo, RowNo: integer);
 procedure ChangeSequence(sTransid, FieldName, Prefix: String);
 procedure OnFindRecord(SQLName, SearchField, SearchValue: String; var resultstr: String);
 procedure OnFireSql(SQLName, SqlText: String);
 procedure SQLGETValue(SQLName, FieldName: String; var ResultStr: String);
 function GetRowValidity(Frameno, RowNo: Integer): Integer;
 function FormatNumber(s: String): String; overload;
 function FormatNumber(fieldname, s: String): String; overload;
 function FormatNumber(tfld:Pfld;s: String): string; overload;
 function EnterField(FName: String; RowNo: Integer): Boolean;
 function ExitField(FName: String; RowNo: integer; FValue: String; Id: Extended): boolean;
 function RefreshField(xfld: pFld; rowno: integer;depcall:boolean=false) : String; overload;
 function RefreshField(fname : String ; rowno : integer) : String ; overload;
 procedure RegisterAfterSave;
 function RefreshAutoGen(ModTable:TXDS;FldName, NewValue: String):String;
 procedure GetParentActiveRow(RowNo, Popindex: Integer);
 procedure DeletePopRows(PopFrmNo: Integer);
 function GetParentActiveRowNo(RowNo, Popindex: Integer): Integer;
 procedure DeleteDetailRows(frmno, RowNo: Integer);
 function IsParentFieldsBound(FrameNo, Rowno: Integer): Boolean;
 procedure FillFieldValue(FieldName:String;rowno:integer);
 Function GetValue(FieldName:String;rowno:integer):String;
 property StoreData : TStoreData read fStoreData write SetStoreData;
 function ValidatePickList(fd: pFld; Value: String; rowno: integer): boolean;
 function PrepareField(xfld: pFld; RowNo:integer=1): pFieldRec;
 function Prepare_Selected_Field(xfld: pFld; visibleDCs : AnsiString ; RowNo:integer=1): string; overload;
 function Prepare_Selected_Field(xfld : pFld; RowNo:integer=1;fval:string='') : String ; overload;

 function FieldParentsBound(xfld: pfld; rowno:integer): boolean;
 function IsFieldEmpty(xFld: pfld; rowno:integer): boolean;
 function LoadDropDown(xfld: pFld; frec:pFieldRec): pFieldRec;
 function EvalExpression(xfld: pFld; frec: pFieldRec): pFieldRec;
 procedure GetParentValue(frmno, rowno: integer; pFields: String;
    sList: TStrings);overload;
 function GetActualRows(frmno: integer; PList: TStringList): String;
 function PopupAutoFill(PopGrid:pPopGrid; rowno: integer) : boolean;
 function EmptyCheck(xFld: pfld; rowno: integer): boolean;
 function FillPopup(FrmNo, rowno: integer): Boolean;
 function FillPopupAll(FrmNo: Integer; rowno: integer): Boolean;
 procedure UpdatePopParentChanges(fd: pFld; v, ov: String; rowno: integer);
 procedure AutoQueryOpen(Q: TXDS; RowNo: integer; FName, fValue: String);
 function FormatNum(s: String; wid, dec: integer): String;

end;

implementation

constructor TValidate.Create(AxPro : TAxProvider);
begin
  axp := AxPro;
  Parser := TProfitEval.create(AxPro);
  Parser.WorkOnStoreData := true;
  Parser.OnSetSequnce := ChangeSequence;
  QueryList := nil;
  QueryList := TList.Create;
  SubTypeField := '*';
  Validation := true;
  FreeParser := true;
  AutoAddToList := false;
  RowList := nil;
  RowList := TStringList.create;
  ParentRowList := nil;
  ParentRowList := TStringList.create;
  ParentList := nil;
  ParentList := TStringList.Create;
  ActualRows := nil;
  ActualRows := TStringList.Create;
  chgrowdcs := nil;
  chgrowdcs := TStringlist.Create;
  chgrownos := nil;
  chgrownos := TStringlist.Create;
  FillOnLoadFlds := nil;
  FillOnLoadFlds := TStringList.Create;
  FromMap := false;
  SubFormCall := false;
  RepExpr := nil;
  workquery:=nil;
  fcheck:=nil;
  fromlist:=nil;
  IsError:=false;
  TrimRows:=false;
  Loading:=false;
  InFrame:=false;
  OnWebSave := false;
  DeleteRowOnError := False;
  FldDependentFields := '';
  WarningMsg := '';
  forpicklist := False;
  FastDataFlag := false;
  NonDynamicFilterSQL := false;
  FormLoadPrepareField := false;
  dependcall := false;
  FromADR := False;
  ForIviewDef := False;
  DeleteTrans := False;
end;

Destructor TValidate.Destroy;
var i:integer;
begin
  if FreeParser then
  begin
   Parser.Destroy;
  end;
  For i := 0 To QueryList.Count - 1 Do
  Begin
    If assigned(TXDS(QueryList[i])) Then
    begin
      TXDS(QueryList[i]).Close;
      TXDS(QueryList[i]).destroy;
    end;
  End;
  if Assigned(QueryList) then
  begin
    QueryList.Clear;
    QueryList.Free;
    QueryList := nil;
  end;
  if assigned(RepExpr) then
  begin
   RepExpr.Close;
   Repexpr.Destroy;
  end;
  if assigned(WorkQuery) then begin
   WorkQuery.Close;
   workquery.destroy;
  end;
  if assigned(fcheck) then
  begin
    fcheck.close;
    fcheck.destroy;
  end;
  if assigned(fromlist) then
  begin
    fromlist.close;
    fromlist.destroy;
  end;
  if assigned(RowList) then
  begin
    RowList.Clear;
    FreeAndNil(RowList);
  end;
  if assigned(ParentRowList) then
  begin
    ParentRowList.Clear;
    FreeAndNil(ParentRowList);
  end;
  if assigned(ParentList) then
  begin
    ParentList.Clear;
    FreeAndNil(ParentList);
  end;
  if assigned(ActualRows) then
  begin
    ActualRows.Clear;
    FreeAndNil(ActualRows)
  end;
  if assigned(chgrowdcs) then
  begin
    chgrowdcs.Clear;
    FreeAndNil(chgrowdcs)
  end;
  if assigned(chgrownos) then
  begin
    chgrownos.Clear;
    FreeAndNil(chgrownos);
  end;
  if assigned(FillOnLoadFlds) then FreeAndNil(FillOnLoadFlds);
  inherited;
end;

Procedure TValidate.SetStoreData(Sd:TStoreData);
begin
 fStoredata := Sd;
 if freeparser then begin
   Parser.StoreData := sd;
   parser.axp:=sdef.axprovider;
 end;
end;

procedure TValidate.FillFieldValue(FieldName:String;rowno:integer);
var k:integer;
begin
 Fieldvalue := '';
 IdValue := 0;
 ZeroValue := true;
 k := StoreData.GetFieldIndex(FieldName, rowno);
 FieldIndex:=k;
 if k=-1 then exit;
 FieldValue     := pFieldRec(StoreData.FieldList[k]).Value;
 IdValue        := pFieldRec(StoreData.FieldList[k]).IdValue;
 ZeroValue := pFieldRec(StoreData.FieldList[k]).ZeroValue;

{******01-08-2002*********gou************Parent Rows has been Registered }
 ParentRowno    := pFieldRec(StoreData.FieldList[k]).ParentRowNo;
 SubFrameNo        := PFieldRec(StoreData.FieldList[k]).FrameNo ;
 ParentFrameNo := 0;
end;

{******01-08-2002*********gou************Parent Rows has been Registered }
Procedure TValidate.RegisterRow;
var
 i,K : integer;
 value : String;
begin
 K := StoreData.SubFrames.IndexOf(inttostr(SubFrameno));
 if K = -1 then Raise Exception.Create('No Parent Frame is available for subframe '+ inttostr(SubFrameno));
 Try
  parentFrameNo := strtoint(StoreData.SubFramesParent[k]);
 Except on e:Exception do
  begin
    if assigned(axp) then  axp.dbm.gf.DoDebug.Log(axp.dbm.gf.Axp_logstr+'\uValidate\RegisterRow - '+e.Message);
    Raise Exception.Create('No Parent Frame is available for subframe '+ inttostr(SubFrameno));
  end;
 end;
 for i:=0 to storedata.fieldlist.count-1 do begin
   if (pFieldRec(StoreData.fieldlist[i]).FrameNo = ParentFrameno) and
     (pFieldRec(StoreData.fieldlist[i]).RowNo   = ParentRowno ) and
     (LastRegisteredRow <> ParentRowno ) then Begin
       if (pFieldRec(StoreData.fieldlist[i]).SourceKey) then
        value := floattostr(pFieldRec(StoreData.fieldlist[i]).idValue)
       else
        value := pFieldRec(StoreData.fieldlist[i]).Value;
       Parser.RegisterVar(pFieldRec(StoreData.fieldlist[i]).FieldName , Char(pFieldRec(StoreData.fieldlist[i]).DataType [1]), Value);
   end;
 end;
 LastRegisteredRow := parentRowno;
end;

Function TValidate.GetValue(FieldName:String;rowno:integer):String;
var k:integer;
    fd:pfld;

begin
 result := '';
 if axp.dbm.gf.IsSysVar(fieldname) then begin
   result := parser.GetVarValue(fieldname);
   exit;
 end;
 if lowercase(FieldName) = 'comp_name' then begin
   result := storedata.companyname;
   exit;
 end;
 result := parser.GetVarValue(fieldname);
 if (rowno = -1) and (lowercase(Parser.LastVarType) = 'c') then
   result := QuotedStr(result);
end;

Function TValidate.GetId(FieldName:String;rowno:integer):extended;
var k:integer;
begin
 Result := 0;
 k := StoreData.GetFieldIndex(FieldName, rowno);
 //SAB 24-jul-02
 if k=-1 then k:=StoreData.GetFieldIndex(FieldName, 1);
 if k=-1 then exit;
 Result := pFieldRec(StoreData.FieldList[k]).IdValue;
end;

Function TValidate.FillAndValidate(TrimEmptyRows:boolean) : Boolean;
Var
  i, bk, rc, x, rx, gx,rcount,chgdcs: integer;
  FieldName, DataType, SaveTableName, BValue, chgrows , trno : String;
  SourceKey, w, m: Boolean;
Begin
  isonline := False;
  isonline := Parser.GetVarValue('Axp_Web_SqlExp_Val_Def_RestRad')='online';
  InFrame:=false;
  axp.dbm.gf.DoDebug.msg('>>Validating ' + StoreData.TransType);
  axp.dbm.gf.DoDebug.msg('>>Frame No 1');
  ErrorStr := '';
  TrimRows := TrimEmptyRows;
  Result := true;
  w := parser.workonstoredata;
  parser.WorkOnStoreData := true;
  Parser.StoreData := StoreData;
  OnSave := Parser.GetVarValue('onsave')='s';
  if (axp.dbm.gf.IsService) and ((not axp.dbm.gf.FillAllFields) and (loading) and (not DeleteTrans)) then
  begin
     fillnonsavefieldvalues;
     exit;
  end;
  for i := 1 to sdef.frames.Count do begin
    if (pfrm(sdef.frames[i-1]).Popup) and (not loading) then begin
      if not FillPopup(i,0) then begin
        axp.dbm.gf.DoDebug.msg(ErrorStr);
        Result := false;
        exit;
      end;
      RefreshPopGrid(i);
      axp.dbm.gf.DoDebug.msg('Time elapsed for popgrid  = dc'+ inttostr(i) + ' - ' + inttostr(millisecondsbetween(now(),starttime)));
    end;
  end;
  IdValue := 0;
  x := 0;
  While (x < sdef.fcount) And (Result) Do Begin
    fld := pfld(sdef.flds[x]);
    if fld.DataType = 'i' then begin
      inc(x);
      continue;
    end;
    FieldName := fld.FieldName;
    error_fieldname := FieldName;
    error_row := '1';
    DataType := fld.DataType;
    SaveTableName := fld.Tablename;
    FrameNo := fld.FrameNo;
    SourceKey := fld.SourceKey;
    chgrows := '';

    StoreData.ShowProgress('Filling dependent : ' + FieldName);
    Parser.RegisterVar('ActiveFrame', 'n', IntToStr(FrameNo));
    Parser.RegisterVar('ActiveRow', 'n', '1');
    Parser.RegisterVar('activefield', 'c', FieldName);

    If not fld.AsGrid Then Begin
      FillFieldValue(FieldName, 1);

      if (lowercase(FieldValue) = 'sourcerecordid') or (lowercase(FieldValue)='parentrecordid') or (lowercase(FieldValue)='primaryrecordid') then begin
       inc(x);
       continue;
      end;

      Parser.registervar(fieldname, datatype[1], fieldvalue);

      if IdValue > 0 then axp.dbm.gf.DoDebug.msg(FieldName+' = ' + FieldValue+' Id = '+FloatToStr(IdValue))
      else axp.dbm.gf.DoDebug.msg(FieldName+' = ' + FieldValue);
      BValue := FieldValue;

      error_fieldname := fld.FieldName;
      error_value := FieldValue;
      if not EnterField(Fld.FieldName, 1) then begin
        axp.dbm.gf.DoDebug.msg(ErrorStr);
        Result := false;
        exit;
      end;

      if not ExitField(Fld.FieldName, 1, FieldValue, IdValue) then begin
        axp.dbm.gf.DoDebug.msg(ErrorStr);
        Result := false;
        exit;
      end;

      if BValue <> FieldValue then begin
        if IdValue > 0 then axp.dbm.gf.DoDebug.msg(FieldName+' after validation = ' + FieldValue+' Id = '+FloatToStr(IdValue))
        else axp.dbm.gf.DoDebug.msg(FieldName+' after validation = ' + FieldValue);
        if (fld.popindex > -1) and (fld.IsParentField) and (not loading) then
          UpdatePopParentChanges(fld,FieldValue,BValue,i);
      end;

      Parser.RegisterVar(FieldName, DataType[1], FieldValue);
      inc(x);

    End Else Begin
      if (axp.dbm.gf.IsService) and (OnWebSave) then
      begin
        chgrows := '';
        chgdcs := chgrowdcs.IndexOf('dc'+inttostr(fld.FrameNo));
        if chgdcs >= 0 then
        begin
          trno := trim(chgrownos.Strings[chgdcs]);
          trno := copy(trim(chgrownos.Strings[chgdcs]),1,length(trno)-1);
          if trno <> quotedstr('*') then  chgrows := chgrownos.Strings[chgdcs];
        end else begin
          inc(x);
          continue;
        end;
      end;
      if (storedata.LastSavedRecordid = 0) then begin
        rcount:=storedata.GetRowCount(fld.FrameNo);
        if (rcount=1) and (GetRowValidity(fld.FrameNo, rcount)=0) then begin
          RegRow(fld.FrameNo,1);
          if pfrm(sdef.frames[fld.FrameNo-1]).PopParent <> -1 then
            DeleteDetailRows(fld.FrameNo,1);
          storedata.deleterow(fld.FrameNo, 1);
          rcount:=0;
        end;
        if (rcount<=0) then begin
          if assigned(ExecuteFillGrid) then
            ExecuteFillGrid(fld.FrameNo);
        end;
      end;
      rx := x;
      rc := StoreData.GetRowCount(fld.FrameNo);
      axp.dbm.gf.DoDebug.msg('>>rc '+inttostr(rc));
      If rc < 1 Then Begin
        if not (sdef.getframe(fld.FrameNo).AllowEmpty) then
        begin
          result := false;
          raise EDatabaseerror.Create(sdef.getframe(FrameNo).caption+' grid in '+sdef.Caption+' should have atleast one row.');
        end;
        inc(x);
        continue;
      End;
      LastRegisteredRow := 0;
      i := 1;
      if chgrows <> '' then axp.dbm.gf.DoDebug.msg('>>Frame No '+IntToStr(FrameNo)+', Changed Rows : '+ chgrows);
      while i <= rc do begin
        if chgrows <> '' then
        begin
          if pos(quotedstr(inttostr(i)),chgrows) <= 0 then
          begin
            inc(i);
            While (x < sdef.fcount) And (FrameNo = pfld(sdef.flds[x]).FrameNo) Do Begin
              inc(x);
            End;
            continue;
          end;
        end;
        x := rx;
        axp.dbm.gf.DoDebug.msg('>>Frame No '+IntToStr(FrameNo)+', Row '+IntToStr(i));
        error_row := inttostr(i);
        if (pfrm(sdef.frames[fld.FrameNo-1]).Popup) then begin
          if not IsParentValueFound(FrameNo,i) then begin
            StoreData.DeleteRow(Frameno, i);
            rc := StoreData.GetRowCount(Frameno);
            continue;
          end;
          GetParentActiveRow(i,pfrm(sdef.frames[fld.FrameNo-1]).popindex);
          if ErrorStr <> '' then begin
            axp.dbm.gf.DoDebug.msg(ErrorStr);
            Result := false;
            exit;
          end;
          ActualRowNo := GetActualRowNo(i,pfrm(sdef.frames[fld.FrameNo-1]).popindex);
          if ActualRowNo = 0 then ActualRowNo := i;
        end else
          Parser.RegisterVar('ActivePRow', 'n', IntToStr(i));
        ValidRowPos := StoreData.GetFieldIndex('validrow'+inttostr(frameno), i);
//        ValidRowPos := sdef.GetFieldIndex('validrow'+inttostr(frameno));
        if (TrimRows) and (GetRowValidity(Frameno, i) = 0) then begin
          RegRow(Frameno,i);
          if pfrm(sdef.frames[FrameNo-1]).PopParent <> -1 then
            DeleteDetailRows(FrameNo,i);
          StoreData.DeleteRow(Frameno, i);
          rc := StoreData.GetRowCount(Frameno);
          continue;
        end;

        While (x < sdef.fcount) And (FrameNo = pfld(sdef.flds[x]).FrameNo) Do Begin
          fld := pfld(sdef.flds[x]);
          FieldName := fld.FieldName;
          DataType := fld.DataType;
          SaveTableName := fld.Tablename;
          SourceKey := fld.SourceKey;
          Parser.RegisterVar('ActiveRow', 'n', IntToStr(i));

          StoreData.ShowProgress('Filling dependent : ' + FieldName + ' Row No. ' + inttostr(i));
          FillFieldValue(FieldName, i);
          if (lowercase(FieldValue) = 'sourcerecordid') or (lowercase(fieldvalue)='parentrecordid') or (lowercase(FieldValue)='primaryrecordid') then begin
           inc(x);
           continue;
          end;
          if (ParentRowno > 0 ) then begin
            axp.dbm.gf.DoDebug.msg('Parent Row = '+IntToStr(ParentRowNo));
            RegisterRow;
          end;
          Parser.registervar(fieldname, datatype[1], fieldvalue);

          axp.dbm.gf.DoDebug.msg(FieldName+' = ' + FieldValue);
          if IdValue > 0 then axp.dbm.gf.DoDebug.msg('Id = '+FloatToStr(IdValue));
          BValue := FieldValue;

          error_fieldname := fld.FieldName;
          error_value := FieldValue;
          if not EnterField(Fld.FieldName, i) then begin
            axp.dbm.gf.DoDebug.msg(ErrorStr);
            Result := false;
            exit;
          end;

          if not ExitField(Fld.FieldName, i, FieldValue, IdValue) then begin
            axp.dbm.gf.DoDebug.msg(ErrorStr);
            Result := false;
            exit;
          end;

          if BValue <> FieldValue then begin
            if IdValue > 0 then axp.dbm.gf.DoDebug.msg(FieldName+' after validation = ' + FieldValue+' Id = '+FloatToStr(IdValue))
            else axp.dbm.gf.DoDebug.msg(FieldName+' after validation = ' + FieldValue);
            if (fld.popindex > -1) and (fld.IsParentField) and (not loading) then
              UpdatePopParentChanges(fld,FieldValue,BValue,i);
          end;
          inc(x);
        End;
        if not result then break;
        ValidRowPos := StoreData.GetFieldIndex('validrow'+inttostr(frameno), i);
        if TrimRows then begin
          if (validrowpos >= 0) and ((uppercase(pFieldRec(StoreData.FieldList[validrowpos]).Value)='F') or (pFieldRec(StoreData.FieldList[validrowpos]).Value=''))  then begin
            RegRow(Frameno,i);
            if pfrm(sdef.frames[FrameNo-1]).PopParent <> -1 then
              DeleteDetailRows(FrameNo,i);
            StoreData.DeleteRow(Frameno, i);
            rc := StoreData.GetRowCount(Frameno);
            If rc < 1 Then Begin
              if not (sdef.getframe(FrameNo).AllowEmpty) then
              begin
                result := false;
                raise EDatabaseerror.Create(sdef.getframe(FrameNo).caption+' grid in '+sdef.Caption+' should have atleast one row.');
              end;
            End;
            continue;
          end;
        end;
        inc(i);
      End;
    End;
  End;
  FillParentRow;
  if (axp.dbm.gf.IsService) and (OnWebSave) then
  begin
    for i := 1 to sdef.frames.Count do begin
      rc := StoreData.GetRowCount(i);
      if (pfrm(sdef.frames[i-1]).AsGrid) and (rc < 1) then begin
        if not (pfrm(sdef.frames[i-1]).AllowEmpty) then
        begin
          error_fieldname := '';
          result := false;
          raise EDatabaseerror.Create(pfrm(sdef.frames[i-1]).caption+' grid in '+sdef.Caption+' should have atleast one row.');
        end;
      end;
    end;
  end;
  parser.workonstoredata := w;
  Loading:=false;
End;


Function TValidate.ValidateData(RowNo:Integer):boolean;
var Expression,m,dt,FValue,s : String;
    j : Integer;
    n : ixmlnode;
begin
  Result := true;
  IsError := true;
  DeleteRowOnError := False;
  WarningMsg := '';
  s := '';
  n := nil;
  if (not Validation) or (Loading) then exit;
  if ((axp.dbm.gf.actionName = 'isave') or (axp.dbm.gf.actionName = 'usave')) and
     (((copy(lowercase(fld.FieldName),1,11) = 'sql_editor_')) or ((copy(lowercase(fld.FieldName),1,11) = 'exp_editor_'))) then exit;

  if (fld.pattern<>'') and  (fieldvalue<>'') and (fld.CustType <> 'Time') and (not axp.dbm.gf.checkmatch(fld.pattern, fieldvalue)) then begin
    Errorstr:= 'The value in '+GetFldPrompt(fld, rowno)+' should be in this format '+fld.pattern;
    result := False;
    exit;
  end;

  If fld.ValExprn >= 0 Then Begin
    axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cvalexp);
    Parser.EvalPrepared(fld.ValExprn);
    m := Parser.value;
    If (lowercase(m) <> 't') and (m<>'') Then begin
      if m[1] = '#' then
      begin
        if assigned(axp.dbm.gf.AxValErrMsgs) then
        begin
           s := 'E' + copy(m, 2, length(m));
           n := axp.dbm.gf.AxValErrMsgs.ChildNodes.FindNode(s);
           if n <> nil then
           begin
              s := vartostr(n.NodeValue);
              if s <> '' then
              begin
                 Errorstr := s;
                 result := False;
                 exit;
              end;
           end
        end;
      end;
      if m[1] <> '_' then
       Result := false
      else begin
       m := copy(m, 2, length(m));
       IsError:=false;
       WarningMsg := GetFldPrompt(fld, rowno)+' '+m;
      end;
      if lowercase(m) = 'f' then
       ErrorStr := 'Invalid input in '+GetFldPrompt(fld, rowno)
      else if not result then begin
       if copy(m,1,2) = '--' then
       begin
         Errorstr := GetFldPrompt(fld, rowno)+Copy(m,3,Length(m));
         DeleteRowOnError := True;
       end else
         Errorstr:=GetFldPrompt(fld, rowno)+m;
       exit;
      end;
    end;
  End;
  If fld.axRule_ValExprn >= 0 Then Begin
    axp.dbm.gf.DoDebug.msg('Evaluating AxRules Validation - '+fld.axRule_cvalexp);
    Parser.EvalPrepared(fld.axRule_ValExprn);
    m := Parser.value;
    If (lowercase(m) <> 't') and (m<>'') Then begin
      if m[1] = '#' then
      begin
        if assigned(axp.dbm.gf.AxValErrMsgs) then
        begin
           s := 'E' + copy(m, 2, length(m));
           n := axp.dbm.gf.AxValErrMsgs.ChildNodes.FindNode(s);
           if n <> nil then
           begin
              s := vartostr(n.NodeValue);
              if s <> '' then
              begin
                 Errorstr := s;
                 result := False;
                 exit;
              end;
           end
        end;
      end;
      if m[1] <> '_' then
       Result := false
      else begin
       m := copy(m, 2, length(m));
       IsError:=false;
       WarningMsg := GetFldPrompt(fld, rowno)+' '+m;
      end;
      if lowercase(m) = 'f' then
       ErrorStr := 'Invalid input in '+GetFldPrompt(fld, rowno)
      else if not result then begin
       if copy(m,1,2) = '--' then
       begin
         Errorstr := GetFldPrompt(fld, rowno)+Copy(m,3,Length(m));
         DeleteRowOnError := True;
       end else
         Errorstr:=GetFldPrompt(fld, rowno)+m;
       exit;
      end;
    end;
  End;
  If fld.axRule_ValOnSave_ValExprn >= 0 Then Begin
    axp.dbm.gf.DoDebug.msg('Evaluating AxRules Validation OnSave - '+fld.axRule_cvalexp);
    Parser.EvalPrepared(fld.axRule_ValOnSave_ValExprn);
    m := Parser.value;
    If (lowercase(m) <> 't') and (m<>'') Then begin
      if m[1] = '#' then
      begin
        if assigned(axp.dbm.gf.AxValErrMsgs) then
        begin
           s := 'E' + copy(m, 2, length(m));
           n := axp.dbm.gf.AxValErrMsgs.ChildNodes.FindNode(s);
           if n <> nil then
           begin
              s := vartostr(n.NodeValue);
              if s <> '' then
              begin
                 Errorstr := s;
                 result := False;
                 exit;
              end;
           end
        end;
      end;
      if m[1] <> '_' then
       Result := false
      else begin
       m := copy(m, 2, length(m));
       IsError:=false;
       WarningMsg := GetFldPrompt(fld, rowno)+' '+m;
      end;
      if lowercase(m) = 'f' then
       ErrorStr := 'Invalid input in '+GetFldPrompt(fld, rowno)
      else if not result then begin
       if copy(m,1,2) = '--' then
       begin
         Errorstr := GetFldPrompt(fld, rowno)+Copy(m,3,Length(m));
         DeleteRowOnError := True;
       end else
         Errorstr:=GetFldPrompt(fld, rowno)+m;
       exit;
      end;
    end;
  End;
  If (fld.NoDuplicate) then begin
   if fld.AsGrid then begin
    result := (FieldValue='') or (DuplicateInGrid(Rowno));
    if not result then begin
      m := Parser.GetVarValue('OnDuplicate_'+fld.FieldName);
      if m <> '' then
        ErrorStr := GetErrorStr(m,RowNo)
      else
        ErrorStr := 'Duplicate values are not allowed in '+ GetFldPrompt(fld, Rowno);
      exit;
    end;
   end else begin
     if FieldValue<>'' then
     begin
       Result := not CheckDuplicate(RowNo);
       if not Result then begin
         ErrorStr := 'Duplicate values are not allowed in '+ GetFldPrompt(fld, rowno);
         exit;
       end;
     end;
   end;
  end;
  if (not fld.Empty) then begin
   dt := lowercase(fld.DataType);
   if (dt = 'n') and (axp.dbm.gf.strtofloatz(fieldvalue) = 0) then
     Result := false
   else if (axp.dbm.gf.Isemptyvalue(fieldvalue, dt) = 'T') then
     Result := false;
   if not result then begin
     if fld.AsGrid then
     begin
       m := Parser.GetVarValue('OnEmpty_'+fld.FieldName);
       if m <> '' then
       begin
         ErrorStr := GetErrorStr(m,RowNo);
         exit;
       end
     end;
     ErrorStr := Getfldprompt(fld, rowno) + ' cannot be left empty.';
     exit;
   end;
  end;
  if fld.OnlyPositive then begin
    if (fld.DataType = 'n') and (axp.dbm.gf.strtofloatz(fieldvalue) < 0) then begin
      Result := False;
       m := Parser.GetVarValue('OnNegative_'+fld.FieldName);
       if m <> '' then
         ErrorStr := GetErrorStr(m,RowNo)
       else
         ErrorStr := Getfldprompt(fld, rowno) + ' cannot be negative';
       exit;
    end;
  end;
  if (fld.Tablename <> '') and (not CheckWidth) then begin
   Result :=  false;
   ErrorStr := GetFldPrompt(fld,rowno) + ' : value exceeds defined width';
   exit;
  end;
  if assigned(fld.qselect) and (fld.qselect.cds.active) and (fieldvalue <> '') then begin
    if fld.txtSelection then begin
      if not ValidatePicklist(fld,fieldvalue,rowno) then
      begin
        result := False;
        exit;
      end;
    end else if (fld.modeofentry = 'select') then begin
      if fld.CommaSelection then begin
        j := 1;
        FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
        while FValue <> '' do begin
          fld.QSelect.CDS.Locate(fld.QSelect.CDS.Fields[fld.searchcol].FieldName, FValue, [locaseinsensitive]);
          if (lowercase(fld.QSelect.CDS.Fields[fld.searchcol].AsString) <> lowercase(FValue)) then begin
            result := False;
            ErrorStr := 'Invalid selection "'+fvalue+'" in '+Getfldprompt(fld,rowno);
            exit;
          end;
          Inc(j);
          FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
        end
      end else begin
        try
        fld.QSelect.CDS.Locate(fld.QSelect.CDS.Fields[fld.searchcol].FieldName, FieldValue, [locaseinsensitive]);
        if (lowercase(fld.QSelect.CDS.Fields[fld.searchcol].AsString) <> lowercase(FieldValue)) then begin
          result := false;
          ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+Getfldprompt(fld, rowno);
        end;
        except on e:exception do
          axp.dbm.gf.DoDebug.msg('cds locate error : ' + e.Message);
        end;
      end;
    end;
  end;
end;

function TValidate.CheckWidth:boolean;
var s,t : String;
    w,p,l,d : integer;
begin
 result := true;
 if fld.ComponentType = 'cl' then exit;
 w := fld.Width;
 d := fld.Dec;
 t := fld.DataType;
 s := Fieldvalue;
 if t = 'c' then begin
  result := length(trim(FieldValue)) <= w;
 end else if t = 'n' then begin
  s := trim(axp.dbm.gf.removecommas(s));
  s := formatnumber(s);
  if s <> '' then
   s := floattostr(abs(strtofloat(s)));
  l := length(s);
  p := pos(axp.dbm.gf.LocDecimalSeparator, s);
  if d = 0 then begin
   result := (l <= w);
   if p <> 0 then result := false;
  end else begin
   if p = 0 then p := l+1;
   result := ((p-1) <= w);
   if result then
    result := ((l-p) <= d);
  end;
 end;
end;

Function TValidate.EnterField(FName:String; RowNo:Integer):Boolean;
var f:pfld;
    p,j,linksearchcol:integer;
    FValue : String;
    RefreshSuggestive, auto, fieldempty:boolean;
begin
  Result := false;
  ErrorStr := '';
  p := Sdef.GetFieldIndex(fname);
  if p=-1 then exit;
  fld := pfld(Sdef.flds[p]);
  if not (fld.AsGrid) then RowNo := 1;
  FillFieldValue(FName, RowNo);

  if (fld.DataType='n') and (axp.dbm.gf.strtofloatz(fieldvalue)=0) then fieldempty:=true
  else if fieldvalue='' then fieldempty:=true
  else fieldempty:=false;
  if (fieldindex>-1) then
  begin
    if (axp.dbm.gf.IsService) then
    begin
       if (OnWebSave) then
         FieldEmpty := (fieldempty) and (OnWebSave) and (pFieldRec(StoreData.Fieldlist[FieldIndex]).AutoValue)
       else FieldEmpty := (fieldempty) and ((fld.Exprn >= 0) or (pFieldRec(StoreData.Fieldlist[FieldIndex]).AutoValue))
    end else FieldEmpty := (fieldempty) and (pFieldRec(StoreData.Fieldlist[FieldIndex]).AutoValue);
  end;
  auto := false;
  if pfrm(sdef.frames[fld.FrameNo-1]).popup then
    Parser.RegisterVar('activerow', 'n', inttostr(ActualRowNo))
  else
    Parser.RegisterVar('activerow', 'n', inttostr(rowno));
  Parser.registervar(fname, Char(fld.datatype[1]), fieldvalue);
  if fld.SourceKey then
    Parser.RegisterVar('id_'+fname, 'n', floattostr(idvalue));

  if (fld.ModeofEntry = 'autogenerate') then begin
    if (not loading) then
      GetAutoGenNo(RowNo)
    else begin
      if axp.dbm.gf.PostAutoGen then
        GetAutoGenData(RowNo);
    end;
  end else if fld.ModeOfEntry = 'accept' then begin
    Query := fld.QSelect;
    if assigned(Query) then begin
      If ((not loading) and ((not fld.Suggestive) or (fieldempty))) or ((loading) and (fld.Tablename='')) Then Begin
        if (FillOnLoadFlds.indexof(fld.Fieldname) >= 0) and (loading) and (fld.Tablename='') then begin
          result := true;
          exit;
        end;
        if fld.DynamicParams then DynamicSQL:=fld.SQL.Text else DynamicSQL:='';
        if fld.Refresh then
          Query.CDS.Active := False;
        QueryOpen(Query, RowNo ,'', fieldvalue);
        if xquery<>fld.QSelect then fld.QSelect:=xQuery;
        Query:=xQuery;
        If fld.DataType = 'c' Then
          FieldValue := Query.CDS.Fields[0].asstring
        Else If fld.DataType = 'n' Then
          FieldValue := Query.CDS.fields[0].asstring
        Else If fld.DataType = 'd' Then
          FieldValue := DateTimeToStr(Query.CDS.fields[0].asdatetime)
        else If fld.DataType = 't' then                                 //ch1
          FieldValue :=Query.CDS.Fields[0].asstring;
        if (fieldvalue<>'') and (copy(fieldvalue,1,2)='++') then
          fieldvalue:=TaxCompute(fld, rowno);
        Parser.registervar(fname, Char(fld.datatype[1]), fieldvalue);
        StoreData.SubmitValue(fname, rowno, fieldvalue, '', 0, 0, 0);
        auto := true;
      End;
    end;
    If (FieldEmpty) Then Begin
      if fld.Exprn > -1 then begin
        axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cexp);
        If Parser.EvalPrepared(fld.Exprn) Then
        begin
          FieldValue := Parser.Value;
          SetUsedQuotedStrProp(Fld);
        end;
        auto := true;
      end;
    End;
  end else if ((fld.ModeOfEntry = 'select') and (not loading) and (not fld.cl_multiselect)) or
              ((axp.dbm.gf.AutoselectCompatibility) and (fld.AutoSelect)) then begin
    if fld.FromList then begin
      Query := fld.QSelect;
      if assigned(Query) then begin
        If (FieldValue = '') and (fld.Exprn >= 0) and (FieldEmpty) Then Begin
         axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cexp);
         If Parser.EvalPrepared(fld.Exprn) Then
          FieldValue := Parser.Value;
        End;
        If (Query.CDS.RecordCount = 1) and (fld.AutoSelect) Then
          FieldValue := Query.CDS.Fields[0].AsString;
        if (FieldValue <> '') then begin
          if fld.CommaSelection then begin
            j := 1;
            FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
            while FValue <> '' do begin
              Query.CDS.Locate(Query.CDS.Fields[0].FieldName, FValue, [locaseinsensitive]);
              if (lowercase(Query.CDS.Fields[0].AsString) <> lowercase(FValue)) then begin
                ErrorStr := 'Invalid selection "'+fvalue+'" in '+getfldprompt(fld,rowno);
                exit;
              end;
              Inc(j);
              FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
            end;
          end else begin
              Query.CDS.Locate(Query.CDS.Fields[0].FieldName, FieldValue, [locaseinsensitive]);
              if (lowercase(Query.CDS.Fields[0].AsString) <> lowercase(FieldValue)) then begin
                ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
                exit;
              end else FieldValue := Query.CDS.Fields[0].AsString;
          end;
        end;
      end;
    end else if fld.txtSelection then begin
      If (FieldValue = '') and (fld.Exprn >= 0)  and (FieldEmpty) Then Begin
        axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cexp);
        If Parser.EvalPrepared(fld.Exprn) Then
          FieldValue := Parser.Value;
        if FieldValue<>'' then begin
          if not ValidatePickList(fld, fieldvalue, rowno) then begin
            ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
            FieldValue:='';
            IdValue:=0;
            exit;
          end;
        end;
      End;
      if (not InFrame) and (FieldValue<>'') and (IdValue=0) then begin
        if not ValidatePickList(fld, fieldvalue, rowno) then begin
          ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
          FieldValue:='';
          IdValue:=0;
          exit;
        end;
      end;
    end else if (assigned(fld.QSelect))then begin
      Query := fld.QSelect;
      if not assigned(Query) then begin
       ErrorStr := 'SQL not defined for field '+ getfldprompt(fld, rowno);
       exit;
      end;
      if fld.DynamicParams then DynamicSQL:=fld.SQL.Text else DynamicSQL:='';
      QueryOpen(Query, RowNo , '', fieldvalue);
      if xquery<>fld.QSelect then fld.QSelect:=xQuery;
      Query:=xQuery;
      If (FieldValue = '') and (fld.Exprn >= 0)  and (FieldEmpty) Then Begin
       axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cexp);
       If Parser.EvalPrepared(fld.Exprn) Then
         FieldValue := Parser.Value;
      End;
      If (Query.CDS.RecordCount = 1) and (fld.AutoSelect) Then begin
        FieldValue := Query.CDS.Fields[fld.searchcol].AsString;
        if fld.SourceKey then
          IdValue := Query.CDS.Fields[0].AsFloat;
      end;
      If (IdValue = 0) And (FieldValue <> '') and (fld.SourceKey) Then Begin
        if fld.CommaSelection then begin
          j := 1;
          FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
          while FValue <> '' do begin
            Query.CDS.Locate(Query.CDS.Fields[1].FieldName, FValue, [locaseinsensitive]);
            if (lowercase(Query.CDS.Fields[1].AsString) <> lowercase(FValue)) then begin
              ErrorStr := 'Invalid selection "'+fvalue+'" in '+getfldprompt(fld, rowno);
              exit;
            end;
            Inc(j);
            FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
          end;
        end else begin
          Query.CDS.Locate(Query.CDS.Fields[1].FieldName, FieldValue, [locaseinsensitive]);
          if (lowercase(Query.CDS.Fields[1].AsString) <> lowercase(FieldValue)) then begin
            ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
            exit;
          end;

        end;
        IdValue := Query.CDS.fields[0].asfloat;
      End;
      if (axp.dbm.gf.FromADR) and (fld.SourceKey) and (IdValue <> 0) then begin  //for normalized field and for ADR
        axp.dbm.gf.QueryLocate(Query.cds, Query.CDS.Fields[0].fieldname, IdValue);
        if Query.CDS.Fields[0].asfloat <> IdValue then begin
          ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
          exit;
        end;
        FieldValue := Query.CDS.fields[1].asstring;
      end else begin
        if (IdValue <> 0) and (FieldValue = '') then begin
          axp.dbm.gf.QueryLocate(Query.cds, Query.CDS.Fields[0].fieldname, IdValue);
          if Query.CDS.Fields[0].asfloat <> IdValue then begin
            ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+getfldprompt(fld, rowno);
            exit;
          end;
          FieldValue := Query.CDS.fields[1].asstring;
        end;
      end;
      if (FieldValue <> '') and (not fld.SourceKey) then begin
        if fld.CommaSelection then begin
          j := 1;
          FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
          while FValue <> '' do begin
            Query.CDS.Locate(Query.CDS.Fields[fld.searchcol].FieldName, FValue, [locaseinsensitive]);
            if (lowercase(Query.CDS.Fields[fld.searchcol].AsString) <> lowercase(FValue)) then begin
              ErrorStr := 'Invalid selection "'+fvalue+'" in '+getfldprompt(fld, rowno);
              exit;
            end;
            Inc(j);
            FValue := axp.dbm.gf.getnthstring(FieldValue,j,fld.Separator);
          end;
        end else begin
          try
          Query.CDS.Locate(Query.CDS.Fields[fld.searchcol].FieldName, FieldValue, [locaseinsensitive]);
          if (lowercase(Query.CDS.Fields[fld.searchcol].AsString) <> lowercase(FieldValue)) then begin
//          if Query.CDS.Fields[fld.searchcol].AsString <> FieldValue then begin   //ch1
            ErrorStr := 'Invalid selection "'+fieldvalue+'" in '+Getfldprompt(fld, rowno);
            exit;
          end else FieldValue := Query.CDS.Fields[fld.searchcol].AsString;
          except on e:exception do
            axp.dbm.gf.DoDebug.msg('cds locate error : ' + e.Message);
          end;
        end;
      end;
    end;
  end else if fld.modeofentry='fill' then begin
    if (axp.dbm.gf.IsService) and (OnWebSave) and (fld.Suggestive) then
    begin
        axp.dbm.gf.DoDebug.msg('Skipping fill field in EnterField : ' + fld.FieldName + ' - ' + FieldValue);
    end else begin
      If ((not loading) and ((not fld.Suggestive) or (fieldempty))) or ((loading) and (fld.Tablename='')) Then Begin
        if (FillOnLoadFlds.indexof(fld.Fieldname) >= 0) and (loading) and (fld.Tablename='') then begin
          result := true;
          exit;
        end;
        axp.dbm.gf.DoDebug.msg('Finding linkfield in fieldenter function');
        j := Sdef.GetFieldIndex(fld.LinkField);
        if j=-1 then begin
          Errorstr := 'Unable to find LinkField for ' +getfldprompt(fld, rowno);
          exit;
        end;
        axp.dbm.gf.DoDebug.msg('Linkfield found and assigning Query from link field in fieldenter function');
        Query := pfld(Sdef.flds[j]).QSelect;
        if pfld(sdef.flds[j]).txtSelection then begin
          if InFrame then exit;
          fValue:=GetValue(fld.LinkField, RowNo);
          LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
          if (not Query.cds.Active) or ((Query.cds.Active) and (lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(FValue))) then begin
              if ValidatePickList(pfld(sdef.flds[j]), fValue, RowNo) then begin
                FieldValue := Query.CDS.fieldbyname(fld.SourceField).asstring;
                IdValue := 0;
              end;
            end else
              FieldValue := Query.CDS.fieldbyname(fld.SourceField).asstring;
          auto := true;
        end else begin
          axp.dbm.gf.DoDebug.msg('Query assigned in fieldenter function');
          if assigned(Query) then begin
            LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
            if pfld(Sdef.flds[j]).DynamicParams then DynamicSQL:=pfld(Sdef.flds[j]).SQL.Text else DynamicSQL:='';
            queryopen(query, rowno, '', parser.GetVarValue(pfld(Sdef.flds[j]).FieldName));
            if xquery<>pfld(Sdef.flds[j]).QSelect then pfld(Sdef.flds[j]).QSelect:=xQuery;
            Query:=xQuery;
            FValue := GetValue(fld.LinkField, RowNo);

            If (FValue <> '') Then Begin
              try
              If (lowercase(FValue) <> lowercase(Query.cds.fields[LinkSearchCol].asstring)) Then
                Query.CDS.Locate(Query.CDS.Fields[LinkSearchcol].FieldName, FValue, [locaseinsensitive]);
              if lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(FValue) then begin
                if not loading then begin
                  ErrorStr := 'Invalid Selection "'+fieldvalue+'" in '+ getfldprompt(pfld(Sdef.flds[j]), rowno);
                  exit;
                end else begin
                  axp.dbm.gf.dodebug.msg('Invalid Selection "'+fieldvalue+'" in '+ getfldprompt(pfld(Sdef.flds[j]), rowno)) //added by sab to loaddata evenif there is error in data
                end;
              end else begin
                FieldValue := Query.CDS.fieldbyname(fld.SourceField).asstring;
                auto := true;
              end;
              except on e:exception do
                axp.dbm.gf.DoDebug.msg('cds locate error : ' + e.Message);
              end;
            End;
          end;
        end;
      end;
    end;
  end else if (fld.ModeOfEntry = 'calculate') and ((not loading) or (fld.tablename = '')) then begin
    axp.dbm.gf.DoDebug.msg('Evaluating - '+fld.cexp);
    If Parser.EvalPrepared(fld.Exprn) Then
    begin
      FieldValue := Parser.Value;
      if (fld.DataType = 'n') and (FieldValue <> '') then FieldValue := floattostr(axp.dbm.gf.RealRound(strtofloat(FieldValue),fld.Dec));
      SetUsedQuotedStrProp(Fld);
    end;
    auto := true;
  end;
  if fld.DataType = 'n' then FieldValue := FormatNumber(FieldValue);
  Parser.registervar(fname, Char(fld.datatype[1]), fieldvalue);
  StoreData.Submitvalue(fname, rowno, fieldvalue, '', idvalue, 0, 0);
  if auto then begin
    FieldIndex:=storedata.getfieldindex(fname, rowno);
    if FieldIndex > -1 then
      pFieldRec(StoreData.fieldlist[FieldIndex]).autovalue:=true;
  end;
  Result := true;
end;

Function TValidate.ExitField(FName:String;RowNo:integer; FValue:String; Id:Extended):boolean;
var p,rno,cpos:integer;
    d:TDateTime;
    popgrid : pPopgrid;
    s,ls,rs : String;
    y,m,d1 : word;
begin
  Result := false;
  ErrorStr := '';
  p := Sdef.GetFieldIndex(fname);
  if p=-1 then exit;
  fld := pfld(Sdef.flds[p]);
  if not (fld.AsGrid) then RowNo := 1;
  FieldValue := FValue;
  IdValue := id;
  if fld.DataType[1]='n' then fvalue := formatnumber(fvalue)
  else if (fld.Datatype[1]='d') and (fvalue<>'') and (Trim(fvalue)<>'/  /')   then  d:=StrToDateTime(fvalue);
  if fld.Datatype[1] = 'd' then
  begin
     if fvalue <> '' then
     begin
       DecodeDate(strtodatetime(fvalue),y,m,d1);
       if y <= 1900 then fvalue := '';
     end;
  end;
  parser.RegisterVar(fname, Char(fld.datatype[1]), fvalue);
  if fld.SourceKey then
    parser.RegisterVar('id_'+fname, 'n', floattostr(idvalue));
  CurRow := RowNo;
  Result := ValidateData(Rowno);

  if (copy(lowercase(FName),1,11) = 'sql_editor_') and (FValue <> '')  then
  begin
    if ForIviewDef then Result := ValidateSQL('',FName,FValue)
    else Result := ValidateSQL(storedata.TransType,FName,FValue);
  end;
  if (copy(lowercase(FName),1,11) = 'exp_editor_') and (FValue <> '')  then
    Result := ValidateExpression(storedata.TransType,FName,FValue);
  storedata.Submitvalue(fname, rowno, fieldvalue, '', idvalue, 0, 0);
end;

function TValidate.FormatNumber(s:String) : string;
begin
 Result := FormatNumber(fld,s);
end;

function TValidate.FormatNum(s:String; wid, dec:integer) : String;
var p, decimal, k:integer;
begin
 Result := s;
 if s='' then exit;
 if pos('E', s) = 0 then begin
   s := Parser.GetNumber(axp.dbm.gf.RemoveCommas(s));
   s:=floattostrf(strtofloat(s), ffFixed, wid, dec);
 end else
   s:=floattostrf(strtofloat(s), ffFixed, wid, dec);
 decimal := dec;
 if decimal = 0 then exit;
 p := pos(axp.dbm.gf.LocDecimalSeparator, s);
 if p = 0 then begin
  if decimal > 0 then
   s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0');
 end else begin
  if decimal > 0 then
   s := copy(s,1,p) + axp.dbm.gf.pad(copy(s, p+1, decimal), decimal, '0')
  else
   s := copy(s, 1, p-1);
 end;
 Result := s;
end;


Function TValidate.GetFieldType(fname:String):String;
var fld1 : pfld;
begin
  Result := 'c';
  if forpicklist then begin
    parser.getvarvalue(fname);
    Result:=parser.lastvartype;
  end
  else begin
    fld1 := sdef.GetField(fname);
    if fld1 <> nil then
      result := fld1.DataType
    else begin
      parser.getvarvalue(fname);
      Result:=parser.lastvartype;
    end;
  end;
end;

Procedure TValidate.PrepareWorkQuery(SQLString: String);
Begin
  workquery := axp.dbm.getxds(workquery);
  WorkQuery.Close;
  WorkQuery.buffered := True;
  WorkQuery.CDS.CommandText := SqlString;
End;

Function TValidate.GetRelevantExpression(PExpressionId: Integer): String;
Var
  I: Integer;
Begin
  Result := '';
  If PExpressionId = 0 Then Exit;
  repexpr := axp.dbm.getxds(repexpr);
  RepExpr.buffered := True;
  RepExpr.CDS.CommandText := 'select * from repexpr where exprid = :i';
  RepExpr.AssignParam(0,inttostr(pExpressionid),'n');
  //  RepExpr.Parambyname('i').AsInteger := pExpressionid;
  RepExpr.Open;
  if not RepExpr.CDS.IsEmpty then
    Result := RepExpr.CDS.Fieldbyname('Expression').asstring
  Else
    Raise EDatabaseerror.Create('Unable find expression');
  RepExpr.Close;
End;

Procedure TValidate.QueryOpen(Q:TXDS; RowNo: integer);
Var
  i, p: integer;
  paramvalue, paramname, fname, t, SQLText,s1: String;
  paramdvalue : TDateTime;
  paramivalue : extended;
  idparam : boolean;
  FuncQry : Boolean;
Begin
  if not Q.buffered then Q.buffered:=true;
  FuncQry := False;
  SQLText := q.CDS.CommandText;
  if (axp.dbm.Connection.DbType = 'postgre') and (lowercase(copy(trim(SQLText),1,4)) = 'call') then
  begin
    SQLText := Trim(SQLText);
    if (lowercase(copy(trim(SQLText),1,8)) = 'callproc') then begin
      Delete(SQLText,1,8);
      SQLText := 'call '+SQLText;
    end else begin
      Delete(SQLText,1,4);
      SQLText := 'select '+SQLText;
    end;
    FuncQry := True;
    Q.CDS.CommandText := SQLText;
    if DynamicSQL <> '' then
      DynamicSQL := SQLText;
  end;
  XQuery:=Q;
  s1 := trim(XQuery.CDS.CommandText);
  if copy(s1,1,1) = ':' then
  begin
     delete(s1,1,1);
     XQuery.CDS.CommandText := GetValue(s1, 1);
  end;
  FillCompanyParams(Q, RowNo);
  Q:=XQuery;
  If Q.cds.Params.count = 0 Then Begin
    If Not Q.cds.Active Then begin
      if axp.dbm.gf.remotelogin then
         Q.open
      else begin
        if (lowercase(copy(trim(SQLText),1,6)) = 'select') and (not FuncQry) then
          Q.Open
        else
          Q.execsql;
      end;
    end;
  End Else Begin
    For i := 0 To Q.cds.Params.Count-1 Do Begin
      paramname := Q.cds.Params[i].Name;
      IdParam := false;
      if copy(paramname,1, 4)='id__' then begin
        IdParam := true;
        paramname := copy(paramname, 5, length(paramname));
      end;
      ParamValue := GetValue(paramname, RowNo);
      if idparam then t:='n' else t:=lowercase(getfieldtype(paramname));
      Q.AssignParam(i, ParamValue, t);
    End;
    if not Q.cds.active then begin
      if axp.dbm.gf.remotelogin then
         Q.open
      else begin
        if (lowercase(copy(trim(SQLText),1,6)) = 'select') and (not FuncQry) then
          Q.Open
        else
          Q.execsql;
      end;
    end;
  End;
End;

Procedure TValidate.QueryOpen(Q:TXDS; RowNo: integer ; FName, fValue : String);
Var
  i, p: integer;
  paramvalue, paramname, t ,s,s1 : String;
  paramdvalue : TDateTime;
  paramivalue : extended;
  idparam : boolean;
Begin
  if not Q.buffered then Q.buffered:=true;
  XQuery:=Q;
  s1 := trim(XQuery.CDS.CommandText);
  if copy(s1,1,1) = ':' then
  begin
     delete(s1,1,1);
     XQuery.CDS.CommandText := GetValue(s1, 1);
  end;
  FillCompanyParams(Q, RowNo,fName,fValue);
  Q:=XQuery;
  If Q.cds.Params.count > 0 Then Begin
    For i := 0 To Q.cds.Params.Count-1 Do Begin
      paramname := Q.cds.Params[i].Name;
      IdParam := false;
      if copy(paramname,1, 4)='id__' then begin
        IdParam := true;
        paramname := copy(paramname, 5, length(paramname));
      end;

      ParamValue := GetValue(paramname, RowNo);
      if idparam then t:='n' else t:=lowercase(getfieldtype(paramname));
      if pos(#$D#$A,ParamValue) > 0  then ParamValue := axp.dbm.gf.FindAndReplace(ParamValue,#$D#$A,' ' + #$D#$A);
      {if paramname = 'recordid' then
      begin
        Q.AssignParam(i,paramvalue, 'r');
        t:='n'
      end
      else  }
        Q.AssignParam(i,paramvalue, t);
    End;
  end;
  If Not Q.cds.Active Then begin
    if axp.dbm.gf.remotelogin then
       Q.open
    else begin
      if lowercase(copy(trim(q.CDS.CommandText),1,6)) = 'select' then
      begin
        if fValue = '~' then
        begin
            q.open;
            exit;
        end;
        //-- for pagination in web
        if Axp.dbm.gf.IsService then
        begin
          axp.dbm.gf.pagination_totalrows := 0;
          if not NonDynamicFilterSQL then
          begin
            if Axp.dbm.gf.pagination_pagesize > 0 then
            begin
              if axp.dbm.gf.pagination_pageno = 1 then
              begin
                if Axp.dbm.gf.DisplayTotRows then
                begin
                   s := q.CDS.CommandText;
                   q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(s,true);
                   if lowercase(s) <> lowercase(q.CDS.CommandText) then
                   begin
                     q.open;
                     axp.dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                     q.close;
                   end;
                   q.CDS.CommandText := s;
                   if axp.dbm.gf.pagination_totalrows > 0 then Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
                end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
              end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
            end;
          end;
        end else
        begin
          if Axp.dbm.gf.pagination_pageno > 0 then
          begin
            if axp.dbm.gf.pagination_pageno = 1 then
            begin
              if Axp.dbm.gf.DisplayTotRows then
              begin
                 s := q.CDS.CommandText;
                 q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(s,true);
                 if lowercase(s) <> lowercase(q.CDS.CommandText) then
                 begin
                   q.open;
                   axp.dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                   q.close;
                 end;
                 q.CDS.CommandText := s;
                 if axp.dbm.gf.pagination_totalrows > 0 then Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
              end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
            end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
          end;
        end;
        Q.Open;
        if axp.dbm.gf.pagination_totalrows = 0 then axp.dbm.gf.pagination_totalrows := q.CDS.RecordCount;
        axp.dbm.gf.DoDebug.Msg('Query opened , Record count : ' + inttostr(q.CDS.RecordCount));
        if NonDynamicFilterSQL then NonDynamicFilterSQL := false;
      end else
        Q.execsql;
    end;
  end;
End;

function TValidate.QueryOpenForPickList(Q:TXDS; RowNo: integer ; FName, fValue : String) : TXDS;
Var
  i, p: integer;
  paramvalue, paramname, t ,s : String;
  paramdvalue : TDateTime;
  paramivalue : extended;
  idparam : boolean;
Begin
  result := Q;
  if not Q.buffered then Q.buffered:=true;
  XQuery:=Q;
  FillCompanyParams(Q, RowNo,fName,fValue);
  Q:=XQuery;
  If Q.cds.Params.count > 0 Then Begin
    For i := 0 To Q.cds.Params.Count-1 Do Begin
      paramname := Q.cds.Params[i].Name;
      IdParam := false;
      if copy(paramname,1, 4)='id__' then begin
        IdParam := true;
        paramname := copy(paramname, 5, length(paramname));
      end;

      ParamValue := GetValue(paramname, RowNo);
      if idparam then t:='n' else t:=lowercase(getfieldtype(paramname));
      {if paramname = 'recordid' then
      begin
        Q.AssignParam(i,paramvalue, 'r');
        t:='n'
      end
      else  }
        Q.AssignParam(i,paramvalue, t);
    End;
  end;
  If Not Q.cds.Active Then begin
    if axp.dbm.gf.remotelogin then
       Q.open
    else begin
      if lowercase(copy(trim(q.CDS.CommandText),1,6)) = 'select' then
      begin
        if fValue = '~' then
        begin
            q.open;
            exit;
        end;
        //-- for pagination in web
        if Axp.dbm.gf.IsService then
        begin
          axp.dbm.gf.pagination_totalrows := 0;
          if not NonDynamicFilterSQL then
          begin
            if Axp.dbm.gf.pagination_pagesize > 0 then
            begin
              if axp.dbm.gf.pagination_pageno = 1 then
              begin
                if Axp.dbm.gf.DisplayTotRows then
                begin
                   s := q.CDS.CommandText;
                   q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(s,true);
                   if lowercase(s) <> lowercase(q.CDS.CommandText) then
                   begin
                     q.open;
                     axp.dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                     q.close;
                   end;
                   q.CDS.CommandText := s;
                   if axp.dbm.gf.pagination_totalrows > 0 then Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
                end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
              end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
            end;
          end;
        end else
        begin
          if Axp.dbm.gf.pagination_pageno > 0 then
          begin
            if axp.dbm.gf.pagination_pageno = 1 then
            begin
              if Axp.dbm.gf.DisplayTotRows then
              begin
                 s := q.CDS.CommandText;
                 q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(s,true);
                 if lowercase(s) <> lowercase(q.CDS.CommandText) then
                 begin
                   q.open;
                   axp.dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                   q.close;
                 end;
                 q.CDS.CommandText := s;
                 if axp.dbm.gf.pagination_totalrows > 0 then Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
              end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
            end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
          end;
        end;
        Q.Open;
        if axp.dbm.gf.pagination_totalrows = 0 then axp.dbm.gf.pagination_totalrows := q.CDS.RecordCount;
        axp.dbm.gf.DoDebug.Msg('Query opened , Record count : ' + inttostr(q.CDS.RecordCount));
        if NonDynamicFilterSQL then NonDynamicFilterSQL := false;
      end else
        Q.execsql;
    end;
  end;
  result := Q;
End;

Procedure TValidate.AutoQueryOpen(Q:TXDS; RowNo: integer ; FName, fValue : String);
Var
  i, p: integer;
  paramvalue, paramname, t ,s : String;
  paramdvalue : TDateTime;
  paramivalue : extended;
  idparam : boolean;
Begin
  if not Q.buffered then Q.buffered:=true;
  XQuery:=Q;
  FillCompanyParams(Q, RowNo,fName,fValue);
  Q:=XQuery;
  If Q.cds.Params.count > 0 Then Begin
    For i := 0 To Q.cds.Params.Count-1 Do Begin
      paramname := Q.cds.Params[i].Name;
      IdParam := false;
      if copy(paramname,1, 4)='id__' then begin
        IdParam := true;
        paramname := copy(paramname, 5, length(paramname));
      end;

      ParamValue := GetValue(paramname, RowNo);
      if idparam then t:='n' else t:=parser.lastvartype;
      {if paramname = 'recordid' then
      begin
        Q.AssignParam(i,paramvalue, 'r');
        t:='n'
      end
      else  }
        Q.AssignParam(i,paramvalue, t);
    End;
  end;
  If Not Q.cds.Active Then begin
    if axp.dbm.gf.remotelogin then
       Q.open
    else begin
      if lowercase(copy(trim(q.CDS.CommandText),1,6)) = 'select' then
      begin
        if fValue = '~' then
        begin
            q.open;
            exit;
        end;
        //-- for pagination in web
        axp.dbm.gf.pagination_totalrows := 0;
        if not NonDynamicFilterSQL then
        begin
          if Axp.dbm.gf.pagination_pagesize > 0 then
          begin
            if axp.dbm.gf.pagination_pageno = 1 then
            begin
              if Axp.dbm.gf.DisplayTotRows then
              begin
                 s := q.CDS.CommandText;
                 q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(s,true);
                 if lowercase(s) <> lowercase(q.CDS.CommandText) then
                 begin
                   q.open;
                   axp.dbm.gf.pagination_totalrows := q.CDS.FieldByName('recno').AsInteger;
                   q.close;
                 end;
                 q.CDS.CommandText := s;
                 if axp.dbm.gf.pagination_totalrows > 0 then Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
              end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
            end else Q.CDS.CommandText := axp.dbm.ChangeSqlForPagination(q.CDS.CommandText,false);
          end;
        end;
        Q.Open;
        if axp.dbm.gf.pagination_totalrows = 0 then axp.dbm.gf.pagination_totalrows := q.CDS.RecordCount;
        if NonDynamicFilterSQL then NonDynamicFilterSQL := false;
      end else
        Q.execsql;
    end;
  end;
End;

function TValidate.InsertFilterCondition(SqlText,FName, FValue : String) : String ;
  var i,l,j,k,m,tpos : integer;
      s,fv,fn : String ;
begin
   result := sqlText;
   if pos('dynamicfilter',lowercase(sqlText)) = 0 then exit;
   l := length(sqltext);
   k := 0;
   if (pos('"',fValue) > 0) and (pos(':',fValue) > 0) then
   begin
     fValue := MakeProperValue(FValue) // if the values contains " and : chars together, its giving catastrophic error on execution. In this case: needs to replace with ::
   end;
   if fName <> '' then
   begin
     k := strtoint(fName);
     while pos('dynamicfilter',lowercase(sqlText)) > 0 do
     begin
       s := '';
       fn := '';
       i := pos('dynamicfilter',lowercase(sqlText));
       s := copy(sqltext,i,l);
       j := pos('}',s);
       s := trim(copy(sqlText,i-1,j));
       delete(s,1,14);
       tpos := pos('~',s);
       if tpos > 0 then
         s := Trim(copy(s,1,tpos-1));
       if axp.dbm.gf.IsService then
       begin
           if k > 0 then fn := axp.dbm.gf.GetNthString(s,k)
           else begin
             {if assigned(fld) then
             begin
               if fld.SourceKey then fn := axp.dbm.gf.GetNthString(s,2)
               else fn := axp.dbm.gf.GetNthString(s,1);
               if fn = '' then  fn := axp.dbm.gf.GetNthString(s,1);
             end else}
             fn := axp.dbm.gf.GetNthString(s,1);
           end;
           axp.dbm.gf.DoDebug.Msg('Field Name : ' + fn);
           axp.dbm.gf.DoDebug.Msg('Field Value : ' + fvalue);
           if FValue <> '~' then
           begin
              if fvalue = '' then
                 fn := ' 1=1 '
              else begin
                if k > 0 then
                begin
                  fv := '%' + FValue + '%';
                  fn := axp.dbm.gf.sqllower+'('+fn+')' + ' like ' + quotedstr(lowercase(fv))
                end else begin
                   fv := FValue;
                   fn := axp.dbm.gf.sqllower+'('+fn+')' + ' = ' + quotedstr(lowercase(fv)) ;
                end;
              end;
           end else fN := ' 1=0 ';
       end else begin
           if fname <> '' then fn := axp.dbm.gf.GetNthString(s,strtoint(fname)) ;
           axp.dbm.gf.DoDebug.Msg('Field Name : ' + fN);
           if (FValue <> '') and (fn<>'') then
           begin
              fv := FValue ;
              fN := axp.dbm.gf.sqllower+'('+fN+')' + ' = ' + quotedstr(lowercase(fv)) ;
           end else fN := ' 1=1 ';
       end;
       if  pos('where',lowercase(sqlText)) > 0 then
           fN := ' and ' + fN
       else
            fN := ' where ' + fN ;
       delete(sqlText,i-1,j+1);
       insert(fN,sqltext,i);
     end;
   end else begin
     while pos('dynamicfilter',lowercase(sqlText)) > 0 do
     begin
       s := '';
       fName := '';
       i := pos('dynamicfilter',lowercase(sqlText));
       s := copy(sqltext,i,l);
       j := pos('}',s);
       fName := trim(copy(sqlText,i-1,j));
       delete(fName,1,14);
       tpos := pos('~',fName);
       if tpos > 0 then
         fName := Trim(copy(fName,1,tpos-1));
//       if fld.SourceKey then fName := GetNthString(fName,2)
//       else fName := GetNthString(fName,1);
       fName := axp.dbm.gf.GetNthString(fName,1);
       if (FValue <> '') or (axp.dbm.gf.IsService) then
       begin
         fv := FValue ;
         fName := axp.dbm.gf.sqllower+'('+fName+')' + ' = ' + quotedstr(lowercase(fv)) ;
       end;
       if  pos('where',lowercase(sqlText)) > 0 then
           fName := ' and ' + fName
       else
            fName := ' where ' + fName ;
       delete(sqlText,i-1,j+1);
//       if fValue <> '' then insert(fName,sqltext,i);
       if (FValue <> '') or (axp.dbm.gf.IsService) then insert(fName,sqltext,i);
     end;
   end;
   result := sqlText;
end;

Procedure TValidate.FillCompanyParams(pQuery: TXDS; RowNo: integer);
Var
  p1, p2, p,rc,i: integer;
  S, CName, CValue, LastDataType, fname,val: String;
  fld1 : pfld;
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
      fld1 := sdef.GetField(cName);
      if assigned(fld1) then
      begin
        if sdef.getframe(fld1.FrameNo).AsGrid then begin
          rc := Storedata.GetRowCount(fld1.FrameNo);
          cValue := '';
          for i := 1 to rc do begin
            val := Storedata.GetFieldValue(Cname, i);
            if (i = 1) then
              LastDataType := uppercase(fld1.DataType);
            If (LastDataType = 'N') then begin
              if (val = '') Then val := '0' else val := axp.dbm.gf.RemoveCommas(val);
              cValue := cValue+','+ val;
            end else
              cValue := cValue+','+ axp.dbm.gf.GetQuotedStr(val,fld1.Separator);
          end;
          Delete(cValue,1,1);
        end else begin
          val := Storedata.GetFieldValue(fld1.FieldName, 1);
          if fld1.DataType = 'n' then
            cValue := val
          else
            cValue := axp.dbm.gf.GetQuotedStr(val,fld1.Separator);
        end;
      end else CValue := GetValue(Cname, RowNo);
      if cValue = '' then
      begin
        LastDataType := uppercase(GetFieldType(cname));
        if LastDataType = 'N' then
          cvalue := '0'
        else
          cValue := QuotedStr(cValue);
      end;
    end else begin
      CValue := GetValue(Cname, RowNo);
      LastDataType := uppercase(GetFieldType(cname));
      If (LastDataType = 'N') then begin
        if (CValue = '') Then CValue := '0' else CValue := axp.dbm.gf.RemoveCommas(CValue);
      end;
    end;
    axp.dbm.gf.DoDebug.msg(CName + ' = ' + CValue);
    Delete(S, p1, p2 - p1 + 1);
    Insert(CValue, S, p1);
  End;
  S := FillCompositeParams(S);
  If uppercase(Trim(pQuery.CDS.CommandText)) <> uppercase(trim(S)) Then Begin
    pQuery.Close;
    pQuery.CDS.CommandText := S;
  End;
  DynamicSQL:='';
End;

Procedure TValidate.FillCompanyParams(pQuery: TXDS; RowNo: integer ; fieldName,Fvalue : String);
Var
  p1, p2, p,rc,i: integer;
  S, CName, CValue, LastDataType, fname ,val ,t,paramname,paramvalue: String;
  fld1 : pfld;
  tQ : TXDS;
Begin
  tQ := nil;
  S := DynamicSQL;
  if S = '' then exit;
  S := axp.dbm.gf.FindAndReplace(S,#$D#$A,' ');
  S := axp.dbm.gf.FindAndReplace(S,#$A,' ');
  S := axp.dbm.gf.RemoveExtraSpaces(S);
//  s := InsertFilterCondition(s, fieldName, FValue);
  While true Do Begin
    p1 := pos('{', S);
    If p1 = 0 Then break;
    p2 := pos('}', S);
    If p2 = 0 Then
      Raise EDataBaseError.Create('Invalid SQL');
    CName := Trim(Copy(S, p1 + 1, p2 - p1 - 1));
    if copy(cName,Length(cName),1) = '*' then begin
      Delete(cName,Length(cName),1);
      fld1 := sdef.GetField(cName);
      if assigned(fld1) then
      begin
        if sdef.getframe(fld1.FrameNo).AsGrid then begin
          rc := Storedata.GetRowCount(fld1.FrameNo);
          cValue := '';
          for i := 1 to rc do begin
            val := Storedata.GetFieldValue(Cname, i);
            if (i = 1) then
              LastDataType := uppercase(fld1.DataType);
            If (LastDataType = 'N') then begin
              if (val = '') Then val := '0' else val := axp.dbm.gf.RemoveCommas(val);
              cValue := cValue+','+ val;
            end else
            begin
              if (val = '') then continue;
              if not fld1.UsedQuotedStr then
              begin
                val := axp.dbm.gf.FindAndReplace(val, '''', '''');
                val := axp.dbm.gf.FindAndReplace(val, '"', '""');
              end;
              if fld1.Separator <> '' then
                 cValue := cvalue+','+axp.dbm.gf.GetQuotedStr(val,fld1.Separator)
              else cValue := cValue+','+ quotedstr(val);
            end;
          end;
          Delete(cValue,1,1);
          if cValue = '' then
          begin
            if fld1.DataType = 'n' then
              cvalue := '0'
            else
              cValue := QuotedStr(cValue);
          end;
        end
        else
        begin
          CValue := GetValue(Cname, RowNo);
          if cValue = '' then
          begin
            if fld1.DataType = 'n' then
              cvalue := '0'
            else
              cValue := QuotedStr(cValue);
          end
          else
          begin
            if (fld1.ComponentType = 'cl') or (fld1.cl_multiselect) then
            begin
              if fld1.DataType = 'c' then
              begin
                 if fld1.Separator <> '' then
                    cValue := axp.dbm.gf.GetQuotedStr(CValue,fld1.Separator)
                 else cValue := quotedstr(CValue);
              end;
            end else
            begin
              if not fld1.UsedQuotedStr then
              begin
                CValue := axp.dbm.gf.FindAndReplace(CValue, '''', '''');
                CValue := axp.dbm.gf.FindAndReplace(CValue, '"', '""');
              end else begin
                if fld1.DataType = 'c' then
                begin
                   if fld1.Separator <> '' then
                      cValue := axp.dbm.gf.GetQuotedStr(CValue,fld1.Separator)
                   else cValue := quotedstr(CValue);
                end;
              end;
            end;
          end;
        end;
      end else
      begin
        CValue := GetValue(Cname, RowNo);
        LastDataType := uppercase(GetFieldType(cname));
        If (LastDataType = 'N') then
          if (CValue = '') Then CValue := '0'
        else
          cValue := axp.dbm.gf.GetQuotedStr(CValue,',');
      end;
    end else begin
      fld1 := sdef.GetField(cName);
      CValue := GetValue(Cname, RowNo);
      LastDataType := uppercase(GetFieldType(cname));
      If (LastDataType = 'N') then begin
        if (CValue = '') Then CValue := '0' else CValue := axp.dbm.gf.RemoveCommas(CValue);
      end
      else
      begin
        if assigned(fld1) then
        begin
          if (not fld1.UsedQuotedStr) then
          begin
            CValue := axp.dbm.gf.FindAndReplace(CValue, '''', '''''');
            CValue := axp.dbm.gf.FindAndReplace(CValue, '"', '""');
          end;
        end else
        begin
          CValue := axp.dbm.gf.FindAndReplace(CValue, '''', '''''');
          CValue := axp.dbm.gf.FindAndReplace(CValue, '"', '""');
        end;
      end;
    end;
    axp.dbm.gf.DoDebug.msg(CName + ' = ' + CValue);
    Delete(S, p1, p2 - p1 + 1);
    Insert(CValue, S, p1);
  End;
  S := FillCompositeParams(S);
//  If (uppercase(Trim(pQuery.CDS.CommandText)) <> uppercase(trim(S))) and (not pQuery.CDS.Active) Then Begin
  If (uppercase(Trim(pQuery.CDS.CommandText)) <> uppercase(trim(S))) Then
  Begin
    tQ := axp.dbm.GetXDS(tQ);
    tQ.buffered := true;
    tQ.CDS.CommandText := S;
    For i := 0 To tQ.cds.Params.Count-1 Do
    Begin
      paramname := tQ.cds.Params[i].Name;
      t:=lowercase(getfieldtype(paramname));
      if t = 'd' then
      begin
        ParamValue := GetValue(paramname, RowNo);
        tQ.AssignParam(i,paramvalue, t);
      end;
    End;
    if (uppercase(Trim(pQuery.CDS.CommandText)) <> uppercase(trim(tQ.CDS.CommandText))) then
    begin
      //Because of "Outof present range" bug, below code introduced after discussion with sab
      pQuery.Close;
      axp.dbm.gf.DoDebug.Msg('Query component recreating again');
      pQuery.destroy;
      pQuery := nil;
      pQuery := axp.dbm.GetXDS(pQuery);
      pQuery.buffered := true;
      XQuery:=pQuery;
      //
      pQuery.CDS.CommandText := S;
    end;
  End;
  DynamicSQL:='';
  if assigned(tQ) then
  begin
    tQ.close;
    FreeAndNil(tQ);
  end;
End;

Function TValidate.FillCompositeParams(S: String): String;
Var
  p1,tp1, p2, p: integer;
  CName, CValue, fname: String;
Begin
  p1 := pos('(:', S);
  While p1 > 0 Do Begin
    p2 := pos(')', copy(S,p1+1,20000));
    p2 := p1+p2;
    CName := Copy(S, p1 + 2, p2 - p1 - 2);
    CValue := '(' + GetValue(CName, -1) + ')';
    If cValue = '()' Then cValue := '('' '')';
    axp.dbm.gf.DoDebug.msg(CName + ' = ' + CValue);
    Delete(S, p1, p2 - p1 + 1);
    Insert(CValue, S, p1);
    p1 := pos('(:', S);
  End;
  Result := S;
End;

Function TValidate.GetAllValues(FName:String):String;
var i:integer;
    s:String;
begin
 fname := trim(lowercase(fname));
 result := '';
 for i:=0 to storedata.fieldlist.count-1 do begin
  if trim(lowercase(pFieldRec(StoreData.fieldlist[i]).Fieldname)) = fname then begin
    s := pFieldRec(StoreData.fieldlist[i]).value;
    If s = '' Then continue;
    If (lowercase(pFieldRec(StoreData.fieldlist[i]).datatype) = 'c') Or
            (lowercase(pFieldRec(StoreData.fieldlist[i]).datatype) = 'd') Then
      Result := Result + ',''' + s + ''''
    Else
      Result := Result + ',' + s;
  end;
 end;
 if result <> '' then begin
  Result:=Trim(Result);
  Delete(Result,1,1);
 end;
end;

Procedure TValidate.GetAutoGenNo(RowNo:Integer);
var s:String;
Begin
  if fieldvalue<>'' then begin
    if fieldvalue[1]=':' then begin
      s:=fieldvalue;delete(s,1,1);
      storedata.setprefix('', fld.fieldname, s);
      fieldvalue:=storedata.getlastno(fld.FieldName, true);
    end else if frommap then
    begin
      storedata.noautogen(fld.fieldname);
      if axp.dbm.gf.PostAutoGen then
        GetAutoGenData(RowNo);
    end;
  end else
    fieldvalue:=storedata.getlastno(fld.FieldName, true);

{  If length(FieldValue) > 4 Then begin
    if FromMap then
      StoreData.NoAutoGen(fld.FieldName);
    exit;
  end;
  if FieldValue <> '' then
    StoreData.SetPrefix('', fld.FieldName, FieldValue);
  if (Not(StoreData.UserPrefix))  then
    FieldValue := StoreData.GetLastNo(fld.FieldName, true);}
End;

Function TValidate.CheckDuplicate(RowNo: integer): Boolean;
var TName, idfield, fname , orgval : String;
    i : integer;
Begin
  Result := true;
  fname := fld.FieldName;
  If (fld.NoDuplicate) and (not fld.AsGrid) Then Begin

    if ((fld.ModeofEntry = 'autogenerate') and (pos(';'+fname+';', Storedata.NoAutoGenStr) = 0))
      or (fld.Tablename = '') then begin
      result := false;
      exit;
    end;
    orgval := FieldValue;
    if fld.EncryptValue then orgval := axp.dbm.gf.EncryptFldValue(FieldValue,fld.datatype);
    fcheck := axp.dbm.getxds(fcheck);
    Fcheck.Close;
    Fcheck.buffered := True;
    TName := StoreData.CompanyName + fld.Tablename;
    IdField := fld.Tablename + 'ID';
    If fld.DataType = 'c' Then Begin
      If IdValue = 0 Then Begin
        Fcheck.CDS.CommandText := 'Select ' + fld.FieldName +' from ' + {fcheck.quote(TName)}TName + ' where '+axp.dbm.gf.sqllower+'(' + fld.FieldName + ') = :S';
        fCheck.AssignParam(0,Trim(lowercase(orgval)),'c');
      End Else Begin
        Fcheck.CDS.CommandText := 'Select ' + fld.FieldName +' from ' + {fcheck.quote(TName)}TName + ' where ' + fld.FieldName + ' = :S';
        fCheck.AssignParam(0,Floattostr(IdValue),'n');
      End;
    End Else If fld.DataType = 'n' Then Begin
        Fcheck.CDS.CommandText := 'Select ' + fld.FieldName +' from ' + {fcheck.quote(TName)}TName + ' where ' + fld.FieldName + ' = :S';
        fCheck.AssignParam(0,orgval,'n');
    End Else If fld.DataType = 'd' Then Begin
        Fcheck.CDS.CommandText := 'Select ' + fld.FieldName +' from ' + {fcheck.quote(TName)}TName + ' where ' + fld.FieldName + ' = :S';
        if orgval = '' then
          fCheck.AssignParam(0,axp.dbm.gf.DummyDate,'d')
        else
          fCheck.AssignParam(0,orgval,'d');
    End;
    If StoreData.LastSavedRecordId <> 0 Then
      Fcheck.CDS.CommandText := Fcheck.CDS.CommandText + ' and ' + IdField + ' <> ' +
        FloatToStr(StoreData.GetParentDocid(fld.FrameNo, RowNo));
    Fcheck.Open;
    Result := Not fCheck.cds.IsEmpty;
    if assigned(Fcheck.pnode) then begin
      for i := 0 to FCheck.pnode.ChildNodes.count - 1 do
        Fcheck.pnode.ChildNodes.Delete(0);
    end;
    fcheck.close;
    fcheck.free; fcheck := nil;
  end;
End;

function TValidate.DuplicateInGrid(RowNo:integer):Boolean;
var i:integer;
    fname : String;
begin
 result := true;
 fname := trim(lowercase(fld.FieldName));
 for i:=0 to StoreData.fieldlist.count-1 do begin
 //sab 05/07/2002 if (lowercase(pFieldRec(StoreData.fieldlist[i]).Fieldname) = fname) and (pFieldRec(StoreData.fieldlist[i]).rowno <> rowno) and (lowercase(trim(pFieldRec(StoreData.fieldlist[i]).Value)) <> trim(lowercase(FieldValue))) then begin
  if pFieldRec(StoreData.Fieldlist[i]).RowNo = -1 then continue;
  if (trim(lowercase(pFieldRec(StoreData.fieldlist[i]).Fieldname)) = fname) and (pFieldRec(StoreData.fieldlist[i]).rowno <> rowno) and (lowercase(trim(pFieldRec(StoreData.fieldlist[i]).Value)) = trim(lowercase(FieldValue))) and (pFieldRec(StoreData.fieldlist[i]).ParentRowNo = ParentRowNo) then begin
   result := false;
   break;
  end;
 end;
end;

procedure TValidate.AddToFromListValues(fname:String);
var k : integer;
begin
  PrepareWorkQuery('SELECT MAX(FROMLISTVALUEID) FROM FROMLISTVALUES');
  workquery.open;
  if workquery.isempty then k := 1 else k := WorkQuery.fields[0].asinteger+1;
  workquery.close;

  FromList := axp.dbm.getxds(fromlist);
  fromlist.Append('fromlistvalues');
  FromList.submit('FromListValueid', floattostr(k), 'n');
  FromList.submit('FromListValue', FieldValue, 'c');
  FromList.submit('TransType', Storedata.TransType, 'c');
  FromList.submit('Name', FName, 'c');
  FromList.Post;
end;

procedure TValidate.FillParentRow;
var i, j, fn, rn, pr : integer;
    f, r : String;
begin
  if Storedata.subframes.count=0 then exit;
  for i:=0 to storedata.fieldlist.count-1 do begin
    f := IntToStr(pFieldRec(Storedata.fieldlist[i]).frameno);
    pr := pFieldRec(Storedata.fieldlist[i]).ParentRowNo;
    if (Storedata.subframes.indexof(f) <> - 1) and (pr > 0) then begin
      r := f + ',' + IntToStr(pFieldRec(Storedata.fieldlist[i]).RowNo);
      if RowList.Indexof(r) = -1 then begin
        RowList.Add(r);
        ParentRowList.Add(IntToStr(pr));
      end;
    end;
  end;
  for i:=0 to RowList.count-1 do begin
    fn := StrToInt(axp.dbm.gf.getNthString(rowlist[i], 1));
    rn := StrToInt(axp.dbm.gf.getnthstring(rowlist[i], 2));
    pr := StrToInt(ParentRowList[i]);
    for j:=0 to StoreData.fieldlist.count-1 do begin
      if (pFieldRec(Storedata.fieldlist[j]).Frameno = fn) and (pFieldRec(Storedata.fieldlist[j]).rowno = rn) then
        pFieldRec(Storedata.fieldlist[j]).ParentRowNo := pr;
    end;
  end;
  RowList.clear;
  ParentRowList.Clear;
end;

Procedure TValidate.RegRow(FrameNo, RowNo:integer);
var i, j, k,prow : integer;
    value : String;
    fm : pfrm;
    fd : pFld;
    rowfound : boolean;
begin
 {for i:=0 to storedata.fieldlist.count-1 do begin
   if (pFieldRec(StoreData.fieldlist[i]).FrameNo = FrameNo) and (pFieldRec(StoreData.fieldlist[i]).RowNo   = RowNo ) then Begin
     value := pFieldRec(StoreData.fieldlist[i]).Value;
     Parser.RegisterVar(pFieldRec(StoreData.fieldlist[i]).FieldName , pFieldRec(StoreData.fieldlist[i]).DataType [1], Value);
   end;
 end;}

 fm:=sdef.getframe(frameno);
 Parser.RegisterVar('activerow', 'n', inttostr(rowno));
 prow := StrToInt(Parser.getvarvalue('activeprow'));
 for i := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
   fd:=pFld(sdef.flds[i]);
   rowfound:=false;
   for j := 0 to fd.DataRows.Count-1 do begin
     k:=strtoint(fd.DataRows[j]);
     if pfieldRec(storedata.fieldlist[k]).RowNo=rowno then begin
       value := pFieldRec(StoreData.fieldlist[k]).Value;
       Parser.RegisterVar(pFieldRec(StoreData.fieldlist[k]).FieldName , Char(pFieldRec(StoreData.fieldlist[k]).DataType[1]), Value);
       rowfound:=true;
       break;
     end;
   end;
   if not rowfound then
     Parser.RegisterVar(fd.FieldName , Char(fd.DataType[1]), '');
 end;
 rowfound:=false;
 if fm.popup then begin
   Parser.RegisterVar('activeprow', 'n', inttostr(prow));
   fm:=sdef.getframe(pPopGrid(sdef.popgrids[fm.PopIndex]).ParentFrameNo);
   for i := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
     fd:=pFld(sdef.flds[i]);
     for j := 0 to fd.DataRows.Count-1 do begin
       k:=strtoint(fd.DataRows[j]);
       if pfieldRec(storedata.fieldlist[k]).RowNo=prow then begin
         value := pFieldRec(StoreData.fieldlist[k]).Value;
         Parser.RegisterVar(pFieldRec(StoreData.fieldlist[k]).FieldName , Char(pFieldRec(StoreData.fieldlist[k]).DataType[1]), Value);
         rowfound:=true;
         break;
       end;
     end;
   if not rowfound then
       Parser.RegisterVar(fd.FieldName , Char(fd.DataType[1]), Value);
   end;
 end;
end;

Procedure TValidate.ChangeSequence(sTransid,FieldName, Prefix: String);
Begin
  if FieldName = '' then exit;
  Parser.storedata.setprefix(sTransid, fieldname, prefix);
End;

procedure TValidate.OnFireSql(SQLName, SqlText: String);
var i:integer;
begin
  Query := nil;
  for i:=0 to querylist.count-1 do begin
    if txds(querylist[i]).name='Expr_'+SQLName then begin
      Query := Txds(QueryList[i]);
      break;
    end;
  end;
  if not assigned(Query) then begin
    Query := axp.dbm.GetXDS(Query);
    Query.name:='Expr_'+SQLName;
    Query.buffered:=true;
    Querylist.Add(query)
  end;
  DynamicSQL:=SQLText;
  Query.CDS.CommandText:=sqltext;
  QueryOpen(Query, sdef.activerow);
end;

procedure TValidate.SQLGETValue(SQLName, FieldName: String; var ResultStr: String);
Var
  Q: TXDS;
  Fld : Tfield;
  i:integer;
begin
  Q := nil;
  for i:=0 to querylist.count-1 do begin
    if txds(querylist[i]).name='Expr_'+SQLName then begin
      Q := txds(QueryList[i]);
      q.buffered := True;
      break;
    end;
  end;
  if not assigned(Q) then exit;
  if (Q.CDS.CommandText = '') then Raise Exception.Create('Unable to Evaluate an expression : SQLGet');
  fld:=q.cds.FindField(fieldname);
  if not (Assigned(fld)) then Raise Exception.Create('Unable to Evaluate an expression : SQLGet');
  ResultStr:=Q.cds.fieldbyname(fieldname).AsString;
end;

procedure TValidate.OnFindRecord(SQLName, SearchField, SearchValue: String;var resultstr: String);
Var
  Q: TXDS;
  Fld : Txfield;
  i:integer;
begin
  Q := nil;
  for i:=0 to querylist.count-1 do begin
    if txds(querylist[i]).name='Expr_'+SQLName then begin
      Q := txds(QueryList[i]);
      q.buffered := True;
      break;
    end;
  end;
  if not assigned(Q) then exit;

  if Q.CDS.CommandText = '' then Raise Exception.Create('Unable to Evaluate an expression : SQLGet');
  fld:=Q.FindField(searchfield);
  if not (Assigned(fld)) then Raise Exception.Create('Unable to Evaluate an expression : SQLGet'+searchfield);
  if (Fld.DataType=ftFloat) or  (Fld.DataType=ftInteger) then begin
      if axp.dbm.gf.QueryLocate(Q.CDS,SearchField,strToFloat(SearchValue)) then
          resultstr:='T'
      else
         resultstr:='F';
  end else begin
     if Q.CDS.Locate(SearchField,SearchValue,[locaseinsensitive]) then
        resultstr:='T'
     else resultstr:='F';
  end;
end;

function TValidate.GetRowValidity(FrameNo, Rowno: Integer): Integer;
var k, i : integer;
  EmptyRow, InComplete: Boolean;
begin
  Result := 1;
  if parser.getvarvalue('onsave') = 'l' then exit;
  EmptyRow := true;
  InComplete := false;
  for i := 0 to storedata.fieldlist.count-1 do begin
    if (pFieldRec(StoreData.FieldList[i]).Frameno <> Frameno) or (pFieldRec(StoreData.FieldList[i]).Rowno <> RowNo) then continue;
    k := sdef.GetFieldIndex(pFieldRec(StoreData.FieldList[i]).FieldName);
    if k = -1 then continue;
    if (pFieldRec(StoreData.FieldList[i]).value = '') or ((pFieldRec(StoreData.FieldList[i]).DataType = 'n') and (axp.dbm.gf.StrToFloatz(pFieldRec(StoreData.FieldList[i]).value) = 0)) then begin
      if not pfld(sdef.flds[k]).Empty then Incomplete := true;
    end else begin
      EmptyRow := false;
    end;
  end;
  k := StoreData.GetFieldIndex('validrow'+trim(inttostr(frameno)), rowno);
  if (k>-1) then begin
    if (uppercase(pFieldRec(StoreData.FieldList[k]).Value)='F')  then
       emptyrow := True;
  end;
  if (EmptyRow) then
    Result := 0
  else begin
    if (InComplete) then begin
      Result := -1;
    end;
  end;
end;

function TValidate.FormatNumber(fieldname, s:String) : String;
begin
  fld := sdef.GetField(fieldname);
  result := formatnumber(s);
end;


function TValidate.RefreshField(fname : String ; rowno : integer) : String ;
  var xfld : pFld;
begin
  xfld := sdef.GetField(fname);
  result := RefreshField(xfld,rowno);
end;

function TValidate.RefreshField(xfld : pFld ; rowno : integer;depcall:boolean) : String ;
  var i,j,LinkSearchCol,k : integer;
      v : String;
      fieldempty:boolean;
begin
  if (Sdef.quickload) or (Sdef.QuickDataFlag) then
  begin
    dependcall := depcall;
    result := Prepare_Selected_Field(xfld,rowno);
    exit;
  end;
  result := '';
  axp.dbm.gf.DoDebug.msg('Refresh Field - ' + xfld.FieldName+ ' Row No '+inttostr(rowno));
  v:='';
  k:=storedata.getfieldindex(xfld.fieldname, rowno);

  if k>-1 then begin
    if v = '' then v := pFieldRec(StoreData.Fieldlist[k]).value;;
    if (xfld.DataType='n') and (axp.dbm.gf.strtofloatz(v)=0) then fieldempty:=true
    else if v='' then fieldempty:=true
    else fieldempty:=false;
    if (not axp.dbm.gf.IsService) then  //as discussed with sab
    begin
       FieldEmpty := (fieldempty) and (pFieldRec(StoreData.Fieldlist[k]).AutoValue) ;
       if (xfld.ModeofEntry = 'accept') then FieldEmpty := (pFieldRec(StoreData.Fieldlist[k]).AutoValue);
    end;
  end else FieldEmpty := True;
  result := v;

  if pfrm(sdef.frames[xfld.FrameNo-1]).popup then begin
    GetParentActiveRow(ActualRowno, pfrm(sdef.frames[xfld.frameno-1]).popindex);
    Parser.RegisterVar('activerow', 'n', inttostr(RowNo));
  end else
    Parser.RegisterVar('activerow', 'n', inttostr(rowno));
  if (assigned(xfld.sql)) and  (Trim(xfld.sql.Text)<>'') then
  begin
    if xfld.DynamicParams then DynamicSQL := xfld.SQL.Text else DynamicSQL:='';
    if xfld.txtSelection then begin
      If (result = '') and (xfld.Exprn >= 0) Then Begin
        axp.dbm.gf.DoDebug.msg('Evaluating - '+xfld.cexp);
        If Parser.EvalPrepared(xfld.Exprn) Then
          result := Parser.Value;
      End;
      if result<>'' then begin
        if not ValidatePickList(xfld, result, rowno) then begin
          result:='';
          IdValue:=0;
        end;
        StoreData.SubmitValue(xfld.FieldName, rowno, result, '', IdValue, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
      end;
    end else if xfld.ModeofEntry = 'accept' then
    begin
        if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
        QueryOpen(xfld.QSelect, RowNo,'0',v);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        If xfld.DataType = 'c' Then
          result := xfld.QSelect.CDS.Fields[0].asstring
        Else If xfld.DataType = 'n' Then
          result := xfld.QSelect.CDS.fields[0].asstring
        Else If xfld.DataType = 't' Then
          result := xfld.QSelect.CDS.fields[0].asstring  //ch1
        Else If xfld.DataType = 'd' Then
          if xfld.QSelect.CDS.fields[0].AsString <> '' then
          result := DateTimeToStr(xfld.QSelect.CDS.fields[0].asdatetime)
          else result := xfld.QSelect.CDS.fields[0].AsString;

        if (result<>'') and (copy(result,1,2)='++') then
          result:=TaxCompute(xfld, rowno);

        if xfld.DataType = 'n' then result := FormatNumber(xfld,result);
        StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
    end else
    begin
        if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
        QueryOpen(xfld.QSelect, RowNo , '0',v);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        result := StoreData.GetFieldValue(xfld.FieldName,rowno);
        idValue := StoreData.LastIdValue;
        if (result = '') and (xfld.Exprn > -1) then
        begin
         axp.dbm.gf.DoDebug.msg('Evaluating - '+xfld.cexp);
         If Parser.EvalPrepared(xfld.Exprn) Then
            result := Parser.Value;
        end;
        If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) Then begin
          result := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
          if xfld.SourceKey then
            IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
        end;
        if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, result, [locaseinsensitive]) then begin
           result := '';
           idValue := 0;
        end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
        if xfld.DataType = 'n' then result := FormatNumber(xfld,result);
        StoreData.SubmitValue(xfld.FieldName, rowno, result, '', idvalue, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
    end;
  end else if xfld.ModeofEntry = 'fill' then
  begin
      axp.dbm.gf.DoDebug.msg('Finding linkfield');
      j := Sdef.GetFieldIndex(xfld.LinkField);
      if j=-1 then begin
        Errorstr := 'Unable to find LinkField for ' + GetFldPrompt(xfld, rowno);
        exit;
      end;
      axp.dbm.gf.DoDebug.msg('Linkfield found and assigning Query from link field');
      Query := pfld(Sdef.flds[j]).QSelect;
      if pfld(sdef.flds[j]).txtSelection then begin
        v:=GetValue(xfld.LinkField, RowNo);
        LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
        if (not Query.cds.Active) or ((Query.cds.Active) and (lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(v))) then begin
          if ValidatePickList(pfld(sdef.flds[j]), v, RowNo) then
            result := Query.CDS.fieldbyname(xfld.SourceField).asstring
          else
            result:='';
        end else
            result := Query.CDS.fieldbyname(xfld.SourceField).asstring;
      end else begin
        axp.dbm.gf.DoDebug.msg('Query assigned');
        if assigned(Query) then begin
          LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
          if pfld(Sdef.flds[j]).DynamicParams then DynamicSQL:=pfld(Sdef.flds[j]).SQL.Text else DynamicSQL:='';
          queryopen(query, rowno,'0', storedata.getfieldvalue(xfld.linkfield, rowno));
          if xQuery<>pfld(Sdef.flds[j]).QSelect then pfld(Sdef.flds[j]).QSelect:=xQuery;
          Query:=xQuery;
          result := GetValue(xfld.LinkField, RowNo);
          If (result <> '') Then Begin
            If (lowercase(result) <> lowercase(Query.cds.fields[LinkSearchCol].asstring)) Then
              Query.CDS.Locate(Query.CDS.Fields[LinkSearchcol].FieldName, result, [locaseinsensitive]);
            if lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(result) then begin
              result := ''
            end else
              result := Query.CDS.fieldbyname(xfld.SourceField).asstring;
          End;
        end;
        if xfld.DataType = 'n' then result := FormatNumber(xfld,result);
      end;
      StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
  end else if xfld.ModeofEntry = 'autogenerate' then
  begin
     result := StoreData.GetLastNo(xfld.FieldName,true);
     StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
     Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
  end else if xfld.Exprn > -1 then
  begin
     if (xfld.ModeofEntry = 'calculate') or (FieldEmpty) or (depcall) then begin
       axp.dbm.gf.DoDebug.msg('Evaluating - '+xfld.cexp);
       If Parser.EvalPrepared(xfld.Exprn) Then
       begin
         result := Parser.Value;
         SetUsedQuotedStrProp(xfld);
       end;
        if xfld.DataType <> 'n' then begin
          StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
        end else begin
          if (result <> '') {and (strtofloat(result) <> 0)} then begin  //and condition commented out after discussion with sab
            result := FormatNumber(xfld,result);
            StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
            Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),result);
          end;
        end;
        if (xfld.ModeofEntry = 'accept') and (not axp.dbm.gf.IsService) then begin
          k:=storedata.getfieldindex(xfld.FieldName, rowno);
          if (k > -1) then
            pFieldRec(StoreData.fieldlist[k]).autovalue:=true;
        end;
     end;
  end else begin
    if xfld.DataType = 'n' then begin
      result := FormatNumber(xfld,result);
      StoreData.SubmitValue(xfld.FieldName, rowno, result, '', 0, 0, 0);
    end;
    Parser.RegisterVar(xfld.FieldName, Char(xfld.DataType[1]), result);
  end;
  axp.dbm.gf.DoDebug.msg('Refresh Field Value - ' + result);
end;

Function TValidate.FillPopup(FrmNo:Integer; rowno:integer):Boolean;
var i,j,k,m,prc,rc,pfrmno,arowno,sno: integer;
    popgrid :pPopGrid;
    s,keycols,val : String;
    fQry : TXDS;
    OpenQry,Found : Boolean;
    tfld : pFld;
begin
  Result := True;
  i := pfrm(sdef.frames[FrmNo-1]).Popindex;
  popgrid := pPopgrid(sdef.popgrids[i]);
  if (not assigned(popGrid.AutoFill)) or (Trim(popGrid.AutoFill.Text) = '') or (not popgrid.FirmBind)  then exit;
  fQry := axp.dbm.GetXDS(nil);
  fQry.buffered := True;
  s := popgrid.Parent;
  axp.dbm.gf.DoDebug.msg('Fill popup with firm bind '+ IntToStr(frmNo)+ ' Row '+ IntToStr(RowNo));
  pfrmno := StrToInt(copy(s,3,3));
  try
    fQry.CDS.CommandText := Trim(popgrid.AutoFill.Text);
    if fQry.cds.Params.Count = 0 then begin
      DynamicSQL := fQry.CDS.CommandText;
      QueryOpen(fQry,1);
      OpenQry := False;
    end else
      OpenQry := True;
    if RowNo=0 then begin
      prc := storedata.GetRowCount(pfrmno);
      sno := 1;
    end else begin
      prc := RowNo;
      sno := RowNo;
    end;
    keycols := lowercase(popgrid.keyCols);
    axp.dbm.gf.DoDebug.msg('Popgrid row count : '+ inttostr(Storedata.GetRowCount(popgrid.FrameNo)));
    for i := sno to prc do begin
      RegRow(pfrmno,i);
      if Trim(popGrid.Popcond) <> '' then begin
        axp.dbm.gf.DoDebug.msg('Evaluating - '+popGrid.Popcond);
        Parser.Evaluate(popGrid.Popcond);
        if uppercase(parser.Value)<>'T' then begin
          RegRow(pfrmno,i);
          DeleteDetailRows(pfrmno,i);
          Continue;
        end;
      end;

      if OpenQry then begin
        fQry.close;
        fQry.CDS.CommandText := Trim(popgrid.AutoFill.Text);
        QueryOpen(fQry,i);
      end;
      Parser.Registervar('activeprow', Char('n'), inttostr(i));
      if not IsParentFieldsBound(popgrid.FrameNo,i) then continue;
      GetParentValue(popgrid.FrameNo,i,popgrid.ParentField,ParentList);
      if popgrid.AutoFillFld then
        ParentList.Add('sub'+Trim(inttostr(popgrid.FrameNo))+'_autofill=T');
      ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
      axp.dbm.gf.DoDebug.msg('Popgrid rows : ' + ActualRows.CommaText);
      if fQry.CDS.RecordCount = 0 then begin
        for j := ActualRows.Count-1 downto 0 do
          Storedata.DeleteRow(popgrid.FrameNo,StrToInt(ActualRows[j]));
        continue;
      end;
      j := 0;
      while j <= ActualRows.Count-1 do begin
        arowno := StrToInt(ActualRows[j]);
        fQry.CDS.First;
        while not fQry.CDS.Eof do begin
          Found := True;
          for k := 0 to fQry.CDS.Fields.Count - 1 do begin
            if popgrid.keyCols <> '' then begin
              if pos(','+lowercase(fQry.CDS.Fields[k].FieldName)+',',','+lowercase(popgrid.keyCols)+',') = 0 then continue;
            end;
            tfld := sdef.GetField(fQry.CDS.Fields[k].FieldName);
            if tfld <> nil then begin
              if tfld.DataType = 'n'then begin
                if (storedata.GetFieldValue(tfld.FieldName,arowno) = '') or (StrToFloat(storedata.GetFieldValue(tfld.FieldName,arowno)) = fQry.CDS.Fields[k].AsFloat)  then
                  continue
                else begin
                  Found := False;
                  Break;
                end;
              end else begin
                val := storedata.GetFieldValue(tfld.FieldName,arowno);
                if Trim(lowercase(val)) = Trim(lowercase(fQry.CDS.Fields[k].AsString))  then
                  continue
                else begin
                  Found := False;
                  Break;
                end;
              end;
            end;
          end;
          if Found then break
          else fQry.CDS.Next;
        end;
        if not Found then begin
          Storedata.DeleteRow(popgrid.FrameNo,arowno);
          ActualRows.CommaText := GetActualRows(popgrid.frameno,ParentList);
          continue;
        end else inc(j);
      end;

      fQry.CDS.First;
      m := 0;
      while not fQry.CDS.Eof do begin
         if ActualRows.Count = 0 then begin
          Found := False;
          arowno := Storedata.GetRowCount(popgrid.FrameNo)+1;
          axp.dbm.gf.DoDebug.msg('arowno : '+ inttostr(arowno));
        end else
          Found := True;
        for j := 0 to ActualRows.count-1 do begin
          arowno := StrToInt(ActualRows[j]);
          axp.dbm.gf.DoDebug.msg('arowno : '+ inttostr(arowno));
          for k := 0 to fQry.CDS.Fields.Count - 1 do begin
            if popgrid.keyCols <> '' then begin
              if pos(','+lowercase(fQry.CDS.Fields[k].FieldName)+',',','+lowercase(popgrid.keyCols)+',') = 0 then continue;
            end;
            tfld := sdef.GetField(fQry.CDS.Fields[k].FieldName);
            if tfld <> nil then begin
              if tfld.DataType = 'n'then begin
                if (storedata.GetFieldValue(tfld.FieldName,arowno)='') or (StrToFloat(storedata.GetFieldValue(tfld.FieldName,arowno)) = fQry.CDS.Fields[k].AsFloat)  then begin
                  Found := True;
                  continue;
                end else begin
                  Found := False;
                  Break;
                end;
              end else begin
                val := storedata.GetFieldValue(tfld.FieldName,arowno);
                if Trim(lowercase(val)) = Trim(lowercase(fQry.CDS.Fields[k].AsString))  then begin
                  Found := True;
                  continue;
                end else begin
                  Found := False;
                  Break;
                end;
              end;
            end;
          end;
          if Found then break;
        end;
        if not Found then begin
          axp.dbm.gf.DoDebug.msg('Popgrid rows: ' + ActualRows.CommaText);
          if ActualRows.Count > 0 then begin
            if ActualRows.Count-1 < m then
              arowno := StrToInt(ActualRows[ActualRows.Count-1])+1
            else
              arowno := StrToInt(ActualRows[m])+1;
            storedata.InsertRow(popgrid.FrameNo,arowno);
          end;
          for k := 0 to fQry.CDS.Fields.Count - 1 do begin
            tfld := sdef.GetField(fQry.CDS.Fields[k].FieldName);
            if tfld <> nil then
              storedata.SubmitValue(tfld.FieldName,arowno,fQry.CDS.Fields[k].AsString,'',0,0,0);
          end;
          InsertParentDetails(arowno);
          ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
        end;
        fQry.CDS.Next;
        inc(m);
      end;
    end;
  axp.dbm.gf.DoDebug.msg('FillPopup completed');
  Except
    On E:Exception do begin
      if assigned(axp) then  axp.dbm.gf.DoDebug.Log(axp.dbm.gf.Axp_logstr+'\uValidate\FillPopup - '+e.Message);
      ErrorStr := E.Message;
      result := False;
    end;
  end;
  fQry.close; fQry.Free; fQry := nil;
end;

Function TValidate.FillPopupAll(FrmNo:Integer; rowno:integer):Boolean;
var i,j,k,m,prc,rc,pfrmno,arowno,sno,l: integer;
    popgrid :pPopGrid;
    s,keycols,val : String;
    fQry : TXDS;
    OpenQry,Found,foundparent : Boolean;
    tfld : pFld;
    fm:pfrm;
begin
  Result := True;
  i := pfrm(sdef.frames[FrmNo-1]).Popindex;
  popgrid := pPopgrid(sdef.popgrids[i]);
  if (not assigned(popGrid.AutoFill)) or (Trim(popGrid.AutoFill.Text) = '') or (not popgrid.FirmBind)  then exit;
  fQry := axp.dbm.GetXDS(nil);
  fQry.buffered := True;
  s := popgrid.Parent;
  axp.dbm.gf.DoDebug.msg('Fill popup with firm bind '+ IntToStr(frmNo)+ ' Row '+ IntToStr(RowNo));
  pfrmno := StrToInt(copy(s,3,3));
  fm:=sdef.getframe(FrmNo);
  try
    fQry.CDS.CommandText := Trim(popgrid.AutoFill.Text);
    if fQry.cds.Params.Count = 0 then begin
      DynamicSQL := fQry.CDS.CommandText;
      QueryOpen(fQry,1);
      OpenQry := False;
    end else
      OpenQry := True;
    if RowNo=0 then begin
      prc := storedata.GetRowCount(pfrmno);
      sno := 1;
    end else begin
      prc := RowNo;
      sno := RowNo;
    end;
    keycols := lowercase(popgrid.keyCols);
    axp.dbm.gf.DoDebug.msg('Popgrid row count : '+ inttostr(Storedata.GetRowCount(popgrid.FrameNo)));
    for i := sno to prc do begin
      RegRow(pfrmno,i);
      if Trim(popGrid.Popcond) <> '' then begin
        axp.dbm.gf.DoDebug.msg('Evaluating - '+popGrid.Popcond);
        Parser.Evaluate(popGrid.Popcond);
        if uppercase(parser.Value)<>'T' then begin
          RegRow(pfrmno,i);
          DeleteDetailRows(pfrmno,i);
          Continue;
        end;
      end;

      if OpenQry then begin
        fQry.close;
        fQry.CDS.CommandText := Trim(popgrid.AutoFill.Text);
        QueryOpen(fQry,i);
      end;
      Parser.Registervar('activeprow', 'n', inttostr(i));
      if not IsParentFieldsBound(popgrid.FrameNo,i) then continue;
      GetParentValue(popgrid.FrameNo,i,popgrid.ParentField,ParentList);
      if popgrid.AutoFillFld then
        ParentList.Add('sub'+Trim(inttostr(popgrid.FrameNo))+'_autofill=T');
      ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
      axp.dbm.gf.DoDebug.msg('Popgrid rows : ' + ActualRows.CommaText);
      if fQry.CDS.RecordCount = 0 then begin
        for j := ActualRows.Count-1 downto 0 do
          Storedata.DeleteRow(popgrid.FrameNo,StrToInt(ActualRows[j]));
        continue;
      end;
      j := 0;
      while j <= ActualRows.Count-1 do begin
        arowno := StrToInt(ActualRows[j]);
        fQry.CDS.First;
        while not fQry.CDS.Eof do begin
          Found := True;
          for k := 0 to fQry.CDS.Fields.Count - 1 do begin
            if popgrid.keyCols <> '' then begin
              if pos(','+lowercase(fQry.CDS.Fields[k].FieldName)+',',','+lowercase(popgrid.keyCols)+',') = 0 then continue;
            end;
            tfld := sdef.GetField(fQry.CDS.Fields[k].FieldName);
            if tfld <> nil then begin
              if tfld.DataType = 'n'then begin
                if (storedata.GetFieldValue(tfld.FieldName,arowno) = '') or (StrToFloat(storedata.GetFieldValue(tfld.FieldName,arowno)) = fQry.CDS.Fields[k].AsFloat)  then
                  continue
                else begin
                  Found := False;
                  Break;
                end;
              end else begin
                val := storedata.GetFieldValue(tfld.FieldName,arowno);
                if Trim(lowercase(val)) = Trim(lowercase(fQry.CDS.Fields[k].AsString))  then
                  continue
                else begin
                  Found := False;
                  Break;
                end;
              end;
            end;
          end;
          if Found then break
          else fQry.CDS.Next;
        end;
        if not Found then begin
          Storedata.DeleteRow(popgrid.FrameNo,arowno);
          ActualRows.CommaText := GetActualRows(popgrid.frameno,ParentList);
          continue;
        end else inc(j);
      end;

      fQry.CDS.First;
      m := 0;
      while not fQry.CDS.Eof do begin
         if ActualRows.Count = 0 then begin
          Found := False;
          arowno := Storedata.GetRowCount(popgrid.FrameNo)+1;
          axp.dbm.gf.DoDebug.msg('arowno : '+ inttostr(arowno));
        end else
          Found := True;
        for j := 0 to ActualRows.count-1 do begin
          arowno := StrToInt(ActualRows[j]);
          axp.dbm.gf.DoDebug.msg('arowno : '+ inttostr(arowno));
          for k := 0 to fQry.CDS.Fields.Count - 1 do begin
            if popgrid.keyCols <> '' then begin
              if pos(','+lowercase(fQry.CDS.Fields[k].FieldName)+',',','+lowercase(popgrid.keyCols)+',') = 0 then continue;
            end;
            tfld := sdef.GetField(fQry.CDS.Fields[k].FieldName);
            if tfld <> nil then begin
              if tfld.DataType = 'n'then begin
                if (storedata.GetFieldValue(tfld.FieldName,arowno)='') or (StrToFloat(storedata.GetFieldValue(tfld.FieldName,arowno)) = fQry.CDS.Fields[k].AsFloat)  then begin
                  Found := True;
                  continue;
                end else begin
                  Found := False;
                  Break;
                end;
              end else begin
                val := storedata.GetFieldValue(tfld.FieldName,arowno);
                if Trim(lowercase(val)) = Trim(lowercase(fQry.CDS.Fields[k].AsString))  then begin
                  Found := True;
                  continue;
                end else begin
                  Found := False;
                  Break;
                end;
              end;
            end;
          end;
          if Found then break;
        end;
        if not Found then begin
          axp.dbm.gf.DoDebug.msg('Popgrid rows: ' + ActualRows.CommaText);
          if ActualRows.Count > 0 then begin
            if ActualRows.Count-1 < m then
              arowno := StrToInt(ActualRows[ActualRows.Count-1])+1
            else
              arowno := StrToInt(ActualRows[m])+1;
            storedata.InsertRow(popgrid.FrameNo,arowno);
          end;
          for l := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
             tfld:=pfld(sdef.flds[l]);
             foundparent := false;
             For k := 0 to ParentList.Count-1 do
             begin
                 if ParentList.Names[k] = tfld.FieldName then
                 begin
                   foundparent := true;
                   break;
                 end;
             end;
             if foundparent  then continue;
             if assigned(fQry.CDS.FindField(tfld.FieldName)) then
             begin
                storedata.SubmitValue(tfld.FieldName,arowno,fQry.CDS.FieldByName(tfld.FieldName).AsString,'',0,0,0);
                Parser.RegisterVar(tfld.FieldName,char(tfld.DataType[1]),fQry.CDS.FieldByName(tfld.FieldName).AsString);
             end else begin
                if (tfld.cexp<>'') and (FieldParentsBound(tfld,aRowNo)) then begin
                  If Parser.EvalPrepared(tfld.Exprn) Then begin
                    val := Parser.Value;
                    SetUsedQuotedStrProp(tfld);
                    if tfld.DataType = 'n' then val := FormatNumber(val);
                    StoreData.SubmitValue(tfld.FieldName, aRowNo, val, '', 0, 0, 0);
                    Parser.RegisterVar(tfld.FieldName,char(tfld.DataType[1]),val);
                  end;
                end;
             end;
          end;
          InsertParentDetails(arowno);
          ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
        end;
        fQry.CDS.Next;
        inc(m);
      end;
    end;
  axp.dbm.gf.DoDebug.msg('FillPopup completed');
  Except
    On E:Exception do begin
      if assigned(axp) then  axp.dbm.gf.DoDebug.Log(axp.dbm.gf.Axp_logstr+'\uValidate\FillPopupAll - '+e.Message);
      ErrorStr := E.Message;
      result := False;
    end;
  end;
  fQry.close; fQry.Free; fQry := nil;
end;

Procedure TValidate.GetParentValue(frmno,rowno:integer;pFields:String;sList:TStrings);
var pField,val,fname:String;
    i : Integer;
    tfld : pfld;
begin
  sList.Clear;
  i := 1;
  pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  while pField <> '' do begin
    fname := 'sub'+trim(IntToStr(frmno))+'_'+pField;
    tfld := Sdef.GetField(fname);
    if tfld <> nil then
      fname := tfld.FieldName;
    val := Storedata.GetFieldValue(pField,RowNo);
    if (tfld.DataType = 'n') and (Trim(Val) = '') then val := '0';
    sList.Add(fname+'='+val);
    inc(i);
    pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  end;
end;

Function TValidate.GetActualRows(frmno:integer;PList:TStringList):String;
var rc : Integer;
    i,j : integer;
    ValidRow : Boolean;
    tfld : pFld;
    val : String;
begin
  rc := StoreData.GetRowCount(frmno);
  result := '';
  for i := 1 to rc do begin
    ValidRow := False;
    for j := 0 to PList.Count - 1 do begin
      tfld := Sdef.GetField(PList.Names[j]);
      if tfld.DataType = 'n' then begin
        if (Storedata.GetFieldValue(PList.Names[j],i) = '') or (strtofloat(Storedata.GetFieldValue(PList.Names[j],i)) <> strtofloat(PList.ValueFromIndex[j])) then begin
          ValidRow := False;
          Break;
        end else ValidRow := True;
      end else begin
        if lowercase(Storedata.GetFieldValue(PList.Names[j],i)) <> lowercase(PList.ValueFromIndex[j]) then begin
          ValidRow := False;
          Break;
        end else ValidRow := True;
      end;
    end;
    if ValidRow then result := Result+','+Trim(IntToStr(i));
  end;
  Delete(result,1,1);
end;

Procedure TValidate.DeleteDetailRows(frmno,RowNo:Integer);
var i,popindex : integer;
begin
  for popindex := 0 to sdef.popgrids.Count - 1 do begin
    if (pPopgrid(Sdef.popgrids[popindex]).ParentFrameNo = frmno) then begin
      GetParentValue(pPopgrid(Sdef.popgrids[popindex]).FrameNo,Rowno,pPopgrid(Sdef.popgrids[popindex]).ParentField,ParentList);
      ActualRows.CommaText := GetActualRows(pPopgrid(Sdef.popgrids[popindex]).FrameNo,ParentList);
      for i := ActualRows.Count - 1 downto 0 do begin
        StoreData.DeleteRow(pPopgrid(Sdef.popgrids[popindex]).FrameNo,strToInt(ActualRows[i]));
      end;
    end;
  end;
end;

Procedure TValidate.InsertParentDetails(RowNo:integer);
var i : integer;
begin
  For i := 0 to ParentList.Count-1 do
    Storedata.submitvalue(ParentList.Names[i], Rowno, ParentList.ValueFromIndex[i],'',0,0,0);
end;

Procedure TValidate.GetParentActiveRow(RowNo,Popindex:Integer);
var popgrid : pPopGrid;
    s : String;
    pfrmno,rc,i,j : Integer;
    ValidRow : Boolean;
    tfld : pFld;
    sdval,pval : extended;
begin
  ErrorStr := '';
  popgrid := pPopgrid(sdef.popgrids[popindex]);
  s := popgrid.Parent;
  pfrmno := StrToInt(copy(s,3,3));
  GetParentValue(popgrid.FrameNo,RowNo,popgrid.ParentField);
  rc := StoreData.GetRowCount(pfrmno);
  axp.dbm.gf.dodebug.msg('Parent Row count = '+inttostr(rc));
  for i := 1 to rc do begin
    ValidRow := False;
    for j := 0 to ParentList.Count - 1 do begin
      tfld := Sdef.GetField(ParentList.Names[j]);
      if tfld.DataType = 'n' then begin
        if (Storedata.GetFieldValue(ParentList.Names[j],i)='')   then begin
          ValidRow := False;
          Break;
        end else begin
          sdval := axp.dbm.gf.strtofloatz(Storedata.GetFieldValue(ParentList.Names[j],i));
          if ParentList.ValueFromIndex[j] = '' then
            pval := 0
          else
            pval := axp.dbm.gf.strtofloatz(ParentList.ValueFromIndex[j]);
           if (sdval <> pval) then begin
             ValidRow := False;
             Break;
           end else ValidRow := True;
        end;
      end else begin
        if lowercase(Storedata.GetFieldValue(ParentList.Names[j],i)) <> lowercase(ParentList.ValueFromIndex[j]) then begin
          ValidRow := False;
          Break;
        end else ValidRow := True;
      end;
    end;
    if ValidRow then begin
      RegRow(pfrmno,i);
      Parser.Registervar('activeprow', Char('n'), inttostr(i));
      axp.dbm.gf.dodebug.msg('Parent Row = '+inttostr(i));
      exit;
    end;
  end;
  if not validrow then
    ErrorStr := 'Active parentrow is not found.';
end;

Procedure TValidate.GetParentValue(frmno,rowno:integer;pFields:String);
var pField,val,fname:String;
    i : Integer;
    tfld : pfld;
begin
  ParentList.Clear;
  i := 1;
  pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  while pField <> '' do begin
    fname := 'sub'+trim(IntToStr(frmno))+'_'+pField;
    tfld := Sdef.GetField(pField);
    if tfld <> nil then
      pField := tfld.FieldName;
    val := Storedata.GetFieldValue(fname,RowNo);
    if (tfld.DataType = 'n') and (Trim(Val) = '') then val := '0';
    ParentList.Add(pField+'='+val);
    axp.dbm.gf.dodebug.msg('Parent Values = '+ pField+'='+val );
    inc(i);
    pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  end;
end;

Function TValidate.GetErrorStrForPopGrid(RowNo:Integer):String;
var popgrid : pPopGrid;
    rno,cpos : Integer;
    s,ls,rs : String;
begin
  result := '';
  popgrid := pPopgrid(sdef.popgrids[pfrm(sdef.frames[fld.FrameNo-1]).popindex]);
  GetPopParentValue(fld.FrameNo,RowNo,popgrid.ParentField);
  ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
  rno := ActualRows.IndexOf(Trim(IntToStr(RowNo)));
  if rno > -1 then begin
    s := ErrorStr;
    cpos := pos(' Row ',s);
    ls := copy(s,1,cpos+4);
    delete(s,1,cpos+4);
    cpos := pos('  ',s);
    rs := copy(s,cpos,Length(s));
    ErrorStr := ls+Trim(IntToStr(rno+1))+'('+'Parent Row '+Parser.GetVarValue('activeprow')+')'+rs;
  end;
  result := ErrorStr;
end;

Procedure TValidate.GetPopParentValue(frmno,rowno:integer;pFields:String);
var pField,val,fname:String;
    i : Integer;
    tfld : pfld;
begin
  ParentList.Clear;
  i := 1;
  pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  while pField <> '' do begin
    fname := 'sub'+trim(IntToStr(frmno))+'_'+pField;
    tfld := Sdef.GetField(fname);
    if tfld <> nil then
      fname := tfld.FieldName;
    val := Storedata.GetFieldValue(fname,RowNo);
    if (tfld.DataType = 'n') and (Trim(Val) = '') then val := '0';
    ParentList.Add(fname+'='+val);
    inc(i);
    pField := Trim(axp.dbm.gf.GetNthString(pFields,i));
  end;
end;

function TValidate.GetActualRowNo(RowNo,Popindex:Integer):Integer;
var popgrid : pPopGrid;
    s : String;
    prow : Integer;
begin
  popgrid := pPopgrid(sdef.popgrids[popindex]);
  s := popgrid.Parent;
  prow := StrToInt(Parser.getvarvalue('activeprow'));
  GetParentValue(popgrid.FrameNo,PRow,popgrid.ParentField,ParentList);
  ActualRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
  result := ActualRows.IndexOf(inttostr(Rowno))+1;
end;

procedure TValidate.RegisterAfterSave;
var i:integer;
    v:String;
begin
  for i := 0 to storedata.AutogenFields.count - 1 do begin
    v:=storedata.GetFieldValue(storedata.autogenfields[i],1);
    parser.RegisterVar(storedata.autogenfields[i], Char('c'), v)
  end;
  parser.RegisterVar('recordid', Char('n'), floattostr(storedata.LastSavedRecordId));
end;

Function TValidate.RefreshAutoGen(ModTable:TXDS;FldName, NewValue:String):String;
var fd:pfld;
    i,j,rc,rno,ind : integer;
    tblname,sqltext : String;
begin
  result := '';
  fd:=sdef.GetField(FldName);
  if not assigned(fd) then exit;
  if not assigned(fd.deps) then exit;
  ind := Storedata.GetFieldIndex(FldName,1);
  pFieldRec(Storedata.FieldList[ind]).Value := NewValue;
  Parser.RegisterVar(FldName, Char(fd.Datatype[1]), NewValue);
  if fd.deps.count > 0  then
    axp.dbm.gf.DoDebug.msg('Refreshing dependent fields of '+fd.fieldname);
  ErrorStr := '';
  tblName := lowercase(ModTable.GetTable);
  for i := 0 to fd.deps.count - 1 do begin
    fld:=sdef.GetField(fd.deps[i]);
    if fld = nil then continue;
    if (not fld.AsGrid) then begin
      if (fld.ModeofEntry = 'calculate') or ((fld.ModeofEntry = 'accept') and (not assigned(fld.QSelect)) and (fld.Exprn > -1)) then begin
        Parser.RegisterVar('ActiveRow', Char('n'), '1');
        Parser.RegisterVar('activefield', Char('c'), fld.FieldName);
        RefreshField(fld,1);
        FillFieldValue(Fld.FieldName, 1);
        if not ExitField(Fld.FieldName, 1, FieldValue, IdValue) then begin
          axp.dbm.gf.DoDebug.msg(ErrorStr);
          Result := ErrorStr;
          exit;
        end;
        if lowercase(Fld.Tablename) = tblName then
          ModTable.submit(Fld.FieldName, FieldValue, Fld.Datatype[1]);
      end else
      begin
         if ((fld.ModeofEntry = 'select') or (fld.ModeofEntry = 'accept')) and (assigned(fld.QSelect)) then
         begin
            sqltext := lowercase(fld.QSelect.SqlText);
            if ((pos(lowercase(FldName),sqltext) > 0) and (pos('dual',sqltext) > 0)) then
            begin
              Parser.RegisterVar('ActiveRow', Char('n'), '1');
              Parser.RegisterVar('activefield', Char('c'), fld.FieldName);
              RefreshField(fld,1);
              FillFieldValue(Fld.FieldName, 1);
              if not ExitField(Fld.FieldName, 1, FieldValue, IdValue) then begin
                axp.dbm.gf.DoDebug.msg(ErrorStr);
                Result := ErrorStr;
                exit;
              end;
              if lowercase(Fld.Tablename) = tblName then
                ModTable.submit(Fld.FieldName, FieldValue, Fld.Datatype[1]);
            end;
         end;
      end;
    end else begin
      if (fld.ModeofEntry = 'calculate') or ((fld.ModeofEntry = 'accept')and (not assigned(fld.QSelect)) and (fld.Exprn > -1)) then begin
        rc := storedata.GetRowCount(fld.FrameNo);
        j := 1;
        while j <= rc do begin
          if (pfrm(sdef.frames[fld.FrameNo-1]).Popup) then begin
            GetParentActiveRow(j,pfrm(sdef.frames[fld.FrameNo-1]).popindex);
            if ErrorStr <> '' then begin
              axp.dbm.gf.DoDebug.msg(ErrorStr);
              Result := ErrorStr;
              exit;
            end;
            ActualRowNo := GetActualRowNo(j,pfrm(sdef.frames[fld.FrameNo-1]).popindex);
            if ActualRowNo = 0 then ActualRowNo := j;
            Parser.RegisterVar('activerow', Char('n'), inttostr(ActualRowNo));
            rno := ActualRowno;
          end else begin
            Parser.RegisterVar('ActivePRow', Char('n'), IntToStr(j));
            Parser.RegisterVar('activerow', Char('n'), inttostr(j));
            rno := j;
          end;
          Parser.RegisterVar('activefield', Char('c'), fld.FieldName);
          RegRow(fld.FrameNo,rno);
          RefreshField(fld,rno);
          FillFieldValue(Fld.FieldName, rno);
          if not ExitField(Fld.FieldName, rno, FieldValue, IdValue) then begin
            axp.dbm.gf.DoDebug.msg(ErrorStr);
            Result := ErrorStr;
            exit;
          end;
          if lowercase(copy(fld.FieldName,1,8)) = 'validrow' then begin
            ValidRowPos := StoreData.GetFieldIndex('validrow'+inttostr(frameno), j);
            if TrimRows then begin
              if (validrowpos >= 0) and ((uppercase(pFieldRec(StoreData.FieldList[validrowpos]).Value)='F') or (pFieldRec(StoreData.FieldList[validrowpos]).Value=''))  then begin
                RegRow(fld.Frameno,j);
                if pfrm(sdef.frames[fld.FrameNo-1]).PopParent <> -1 then
                  DeleteDetailRows(fld.FrameNo,j);
                StoreData.DeleteRow(fld.Frameno, j);
                rc := StoreData.GetRowCount(fld.FrameNo);
                If rc < 1 Then Begin
                  if not (sdef.getframe(fld.FrameNo).AllowEmpty) then
                  begin
                    ErrorStr := sdef.getframe(FrameNo).caption+' grid in '+sdef.Caption+' should have atleast one row.';
                    axp.dbm.gf.DoDebug.msg(ErrorStr);
                    result := ErrorStr;
                    Exit;
                  end;
                End;
                continue;
              end;
            end;
          end;
          if lowercase(Fld.Tablename) = tblName then
            ModTable.submit(Fld.FieldName, FieldValue, Fld.Datatype[1]);
          inc(j);
        end;
      end;
    end;
    axp.dbm.gf.DoDebug.msg(' ');
  end;
end;

function TValidate.GetFldPrompt(fld:pFld; RowNo:integer):String;
var
  FieldCaption : String;
begin
  result := '';
  FieldCaption := fld.caption;
  if FieldCaption='' then
    result:= fld.Hint
  else
  begin
    if pos('~',FieldCaption) > 0 then
    begin
      FieldCaption := axp.dbm.gf.FindAndReplace(FieldCaption,'~',' ');
    end;
    result := FieldCaption;
  end;
  if fld.asgrid then begin
    if pfrm(sdef.frames[fld.FrameNo-1]).Popup then
      result := result +' Parent Row '+ Parser.GetVarValue('activeprow') +', Popgrid Row '+IntToStr(ActualRowNo)+' '
    else
      result:=result+' Row '+Inttostr(Rowno)+' ';
  end;
end;

function TValidate.IsParentFieldsBound(FrameNo, Rowno: Integer): Boolean;
var i : integer;
    popgrid : pPopGrid;
    pField,val : String;
begin
  Result := True;
  i := pfrm(sdef.frames[FrameNo-1]).Popindex;
  popgrid := pPopgrid(sdef.popgrids[i]);
  i := 1;
  pField := Trim(axp.dbm.gf.GetNthString(popgrid.ParentField,i));
  while pField <> '' do begin
    val := Storedata.GetFieldValue(pField,RowNo);
    if val = '' then begin
      Result := false;
      break;
    end;
    inc(i);
    pField := Trim(axp.dbm.gf.GetNthString(popgrid.ParentField,i));
  end;
end;

function TValidate.IsParentValueFound(FrameNo,Rowno:integer):Boolean;
var pField,val,fname:String;
    i : Integer;
    tfld : pfld;
    popgrid : pPopGrid;
begin
  result := True;
  i := pfrm(sdef.frames[FrameNo-1]).Popindex;
  popgrid := pPopgrid(sdef.popgrids[i]);
  i := 1;
  pField := Trim(axp.dbm.gf.GetNthString(popgrid.ParentField,i));
  while pField <> '' do begin
    fname := 'sub'+trim(IntToStr(FrameNo))+'_'+pField;
    tfld := Sdef.GetField(pField);
    if tfld <> nil then
      pField := tfld.FieldName;
    val := Storedata.GetFieldValue(fname,RowNo);
    if val = '' then begin
      Result := False;
      break;
    end;
    inc(i);
    pField := Trim(axp.dbm.gf.GetNthString(popgrid.ParentField,i));
  end;
end;

function TValidate.TaxCompute(fd:pfld;rno:integer):String;
var taxcode,basic,rate,formula,amount,tcodes, fcodes:String;
    taxcodes, amtlist:TStringlist;
    rslt:extended;
    i:integer;
begin
  if fd.QSelect.cds.isempty then exit;
  taxcodes:=TStringList.create;
  amtlist:=TStringList.create;
  fd.QSelect.CDS.First;
  fcodes:=trim(axp.dbm.gf.GetNthString(fd.qselect.cds.fields[0].asstring, 2, ' '));
  while not fd.QSelect.cds.eof do begin
    taxcode:=fd.Qselect.cds.fieldbyname('taxcode').asstring;
    basic:=fd.QSelect.cds.fieldbyname('basicamount').asstring;
    rate:=fd.QSelect.cds.fieldbyname('rate').asstring;
    formula:=fd.QSelect.cds.fieldbyname('formula').asstring;
    amount:='0';
    parser.RegisterVar('basicamount', 'n', basic);
    parser.Registervar('rate', 'n', rate);
    parser.Evaluate(formula);
    amtlist.add(parser.Value);
    parser.registervar(taxcode, 'n', parser.value);
    taxcodes.add(taxcode);
    fd.QSelect.cds.next;
  end;
  rslt:=0;
  tcodes:='';
  for I := 0 to amtlist.count-1 do begin
    rslt:=rslt+axp.dbm.gf.strtofloatz(amtlist[i]);
    tcodes:=tcodes+taxcodes[i]+',' +amtlist[i]+',';
  end;
  result:=floattostr(rslt);
  if tcodes<>'' then
    delete(tcodes, length(tcodes), 1);
  if fcodes<>'' then
    storedata.SubmitValue(fcodes, rno, tcodes, '', 0, 0, 0);
  taxcodes.free;
  amtlist.free;
end;


procedure TValidate.DeletePopRows(PopFrmNo:Integer);
var rc,i : integer;
begin
  rc := storedata.GetRowCount(PopFrmNo);
  i := 1;
  while i <= rc do begin
    GetParentActiveRow(i,pfrm(sdef.frames[PopFrmNo-1]).popindex);
    if ErrorStr <> '' then begin
      StoreData.DeleteRow(PopFrmNo, i);
      rc := StoreData.GetRowCount(PopFrmNo);
      continue;
    end;
    inc(i);
  end;
end;

function TValidate.GetParentActiveRowNo(RowNo,Popindex:Integer) : Integer;
var popgrid : pPopGrid;
    s : String;
    pfrmno,rc,i,j : Integer;
    ValidRow : Boolean;
    tfld : pFld;
    sdval,pval : extended;
begin
  Result := 0;
  popgrid := pPopgrid(sdef.popgrids[popindex]);
  s := popgrid.Parent;
  pfrmno := StrToInt(copy(s,3,3));
  GetParentValue(popgrid.FrameNo,RowNo,popgrid.ParentField);
  rc := StoreData.GetRowCount(pfrmno);
  axp.dbm.gf.dodebug.msg('Parent Row count = '+inttostr(rc));
  for i := 1 to rc do begin
    ValidRow := False;
    for j := 0 to ParentList.Count - 1 do begin
      tfld := Sdef.GetField(ParentList.Names[j]);
      if tfld.DataType = 'n' then begin
        if (Storedata.GetFieldValue(ParentList.Names[j],i)='')   then begin
          ValidRow := False;
          Break;
        end else begin
          sdval := axp.dbm.gf.strtofloatz(Storedata.GetFieldValue(ParentList.Names[j],i));
          if ParentList.ValueFromIndex[j] = '' then
            pval := 0
          else
            pval := axp.dbm.gf.strtofloatz(ParentList.ValueFromIndex[j]);
           if (sdval <> pval) then begin
             ValidRow := False;
             Break;
           end else ValidRow := True;
        end;
      end else begin
        if lowercase(Storedata.GetFieldValue(ParentList.Names[j],i)) <> lowercase(ParentList.ValueFromIndex[j]) then begin
          ValidRow := False;
          Break;
        end else ValidRow := True;
      end;
    end;
    if ValidRow then begin
      Result := i;
      axp.dbm.gf.dodebug.msg('Curren Parent Row = '+inttostr(i));
      exit;
    end;
  end;
end;

function TValidate.MakeProperValue(Str: String): String;
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

function TValidate.ValidatePickList(fd:pFld; Value:String; rowno:integer):boolean;
var k,tpgno:integer;
begin
  if not fd.txtSelection then exit;
  fd.QSelect.Close;
  //Because of "Outof present range" bug, below code introduced after discussion with sab
  axp.dbm.gf.DoDebug.Msg('Query component recreating and opening again');
  try
    if assigned(fd.QSelect.CDS) then FreeAndNil(fd.QSelect.CDS);
  except
  end;
  try
    if assigned(fd.QSelect) then FreeAndNil(fd.QSelect);
  except
    fd.QSelect := nil;
  end;
  fd.QSelect := axp.dbm.GetXDS(fd.QSelect);
  fd.QSelect.buffered := true;
  fd.QSelect.CDS.CommandText := ChangeSQL(fd.SQL.Text, Value, fd.pickfields,1);
  //
  if fd.DynamicParams then DynamicSQL:=fd.QSelect.CDS.CommandText else DynamicSQL:='';
  tpgno:=axp.dbm.gf.pagination_pageno;
  axp.dbm.gf.pagination_pageno:=0;
  fd.QSelect := QueryOpenForPickList(fd.QSelect,1,'0',Value);
  XQuery:=fd.QSelect;
  XQuery.cds := fd.QSelect.CDS;
  Query := XQuery ;
  Query.cds := XQuery.cds;
  axp.dbm.gf.pagination_pageno:=tpgno;
  axp.dbm.gf.DoDebug.Msg('Query opened');
  try
    result:=not fd.QSelect.cds.isempty;
    if not result then axp.dbm.gf.DoDebug.Msg('Query opened Dataset is empty')
    else axp.dbm.gf.DoDebug.Msg('Query opened Dataset is not empty');
    if not result then
    begin
      axp.dbm.gf.DoDebug.Msg('ValidatePickList in field : ' + fd.FieldName + 'value : ' + value);
      ErrorStr := 'Invalid selection "'+value+'" in '+getfldprompt(fd, rowno)
    end else begin
      ErrorStr := '';
      if fd.SourceKey then
        IdValue:=fd.QSelect.CDS.Fields[0].AsFloat;
      storedata.submitvalue(fd.FieldName, rowno, value, '', idvalue, 0, 0);
    end;
  except on e:exception do
    begin
      axp.dbm.gf.DoDebug.Msg('ValidatePickList Error : ' + e.Message);
      ErrorStr := 'Invalid selection : ' + e.Message;
    end;
  end;
  axp.dbm.gf.DoDebug.Msg('Query component recreating and opening completed');
end;

function TValidate.ChangeSQL(SqlText,SearchValue:String;SearchFldList:TStringList;SearchIdx:Integer):String;
var l, i, j, k:integer;
    replacewith, s, SearchField,tail, sStr:String;
begin
   result := sqlText;
   if (pos('dynamicfilter',lowercase(sqlText)) = 0) then exit;
   l := length(sqltext);
   if (pos('"',SearchValue) > 0) and (pos(':',SearchValue) > 0) then
     SearchValue := MakeProperValue(SearchValue);
   Parser.RegisterVar('axp_dynamicfilter','c',SearchValue);
   k := -1;
   i := pos('{dynamicfilter',lowercase(sqlText));
   sStr := copy(sqlText,1,i);
   while true do begin
       i := pos('{dynamicfilter',lowercase(sqlText));
       if i=0 then break;
       tail:=copy(sqlText,i,length(sqltext));
       j := pos('}',tail);               /// piclist
       inc(k);
       SearchField := axp.dbm.gf.GetNthString(SearchFldList[k],SearchIdx);
       replacewith := axp.dbm.gf.sqllower+'('+searchfield+')' + '=' +axp.dbm.gf.sqllower+'( :axp_dynamicfilter )' ;
//       if pos('where', lowercase(SQLText)) > 0 then
       if pos('where', lowercase(sStr)) > 0 then
         s:=' and '+ replacewith
       else
         s:=' where ' + replacewith;
       sStr := copy(sqltext,i+j,length(sqltext));
       delete(sqltext, i, j);
       insert(S, SQLText, i);
       i := pos('{dynamicfilter',lowercase(sStr));
       sStr := copy(sStr,1,i);
   end;
   result := sqlText;
end;

Procedure TValidate.UpdatePopParentChanges(fd: pFld; v, ov: String; rowno: integer);
var FName,Fno: String;
    i : integer;
begin
  If not IsParentFieldsBound(pPopGrid(Sdef.popgrids[fd.popindex]).FrameNo,rowno) then exit;
  GetParentValue(pPopGrid(Sdef.popgrids[fd.popindex]).FrameNo,RowNo,pPopGrid(Sdef.popgrids[fd.popindex]).ParentField,ParentList);
  Fno := Trim(IntToStr(pPopGrid(Sdef.popgrids[fd.popindex]).FrameNo));
  FName := 'sub'+Fno+'_'+fd.FieldName;
  ParentList.Values[fName] := ov;
  ActualRows.CommaText := GetActualRows(pPopGrid(Sdef.popgrids[fd.popindex]).FrameNo,ParentList);
  if ActualRows.Count > 0 then begin
    for i := 0 to ActualRows.Count-1 do
    begin
      StoreData.SubmitValue(fName,StrToInt(ActualRows[i]),v,'',0,0,0);
      Parser.RegisterVar(fName , Char(fd.DataType[1]), v);
    end;
  end;
end;
{
procedure TValidate.fillnonsavefieldvalues;
  var i,j,k : integer;
      fd : pFld;
begin
  axp.dbm.gf.DoDebug.msg('Refreshing nonsave fields during loading');
  for i:=0 to sdef.flds.Count -1 do begin
    fd := pfld(sdef.flds[i]);
    if not assigned(fd) then continue;
    if fd.SaveValue then begin
//      if not fd.AsGrid then begin
        parser.RegisterVar(fd.fieldname, Char(fd.DataType[1]), storedata.GetFieldValue(fd.FieldName, 1));
        axp.dbm.gf.DoDebug.msg('Registered '+fd.fieldname+ ' '+storedata.GetFieldValue(fd.FieldName, 1));
//      end;
      continue;
    end;
    axp.dbm.gf.DoDebug.msg('Refreshing ' + fd.FieldName);
    if not fd.AsGrid then
    begin
       RefreshField(fd,1);
    end else
    begin
       j := StoreData.GetRowCount(fd.FrameNo);
       if j = 0  then  j := 1;
       for k := 1 to j do
       begin
        if (pfrm(sdef.frames[fd.FrameNo-1]).Popup) then
            GetParentActiveRow(k,pfrm(sdef.frames[fd.FrameNo-1]).popindex);
         RegRow(fd.frameno, k);
         RefreshField(fd,k) ;
       end;
    end;
  end;
  axp.dbm.gf.DoDebug.msg('Refreshing nonsave fields during loading over');
end;
}

procedure TValidate.fillnonsavefieldvalues;
  var i,r,k,j: integer;
      fd : pFld;
      fm:pFrm;
      frec:pFieldRec;
      fvalue : string;
begin
  axp.dbm.gf.DoDebug.msg('Refreshing nonsave fields during loading');
  for i := 1 to sdef.frames.Count do
  begin
    k := StoreData.RowCount(i);
    if k = 0 then continue;
    fm:=pFrm(sdef.Frames[i-1]);
    for r := 1 to k do begin
      if fm.Popup then
        GetParentActiveRow(r,fm.popindex)
      else
        Parser.Registervar('activeprow', Char('n'), inttostr(r));
      RegRow(i , r);
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        fd:=pFld(sdef.flds[j]);
        if not fd.SaveValue then  RefreshField(fd,r)
        else if (fd.ModeofEntry = 'calculate') and (fd.cexp <> '') then
        begin
          If Parser.EvalPrepared(fd.Exprn) Then begin
            fvalue := Parser.Value;
            SetUsedQuotedStrProp(fd);
            if (fd.DataType = 'n') and (fvalue <> '') then fvalue := floattostr(axp.dbm.gf.RealRound(strtofloat(fvalue),fd.Dec));
            if fd.DataType = 'n' then fvalue := FormatNum(fvalue, fd.Width, fd.Dec);
            StoreData.SubmitValue(fd.FieldName, r, fvalue, '', 0, 0, 0);
            Parser.RegisterVar(fd.FieldName,fd.DataType[1],fvalue);
          end;
        end;
      end;
    end;
  end;
  axp.dbm.gf.DoDebug.msg('Refreshing nonsave fields during loading over');
end;

function TValidate.IsFieldEmpty(xFld:pfld; rowno:integer) : boolean;
var v:String;
    i,k,j:integer;
begin
  v:='';
  k:=-1;
  for I := 0 to xfld.DataRows.count - 1 do begin
    j:=strtoint(xfld.DataRows[i]);
    if xfld.AsGrid then
    begin
      if pFieldRec(storedata.fieldlist[j]).rowno=rowno then begin
        k:=j;
        break;
      end;
    end else
    begin
      k:=j;
      break;
    end;
  end;
  if k>-1 then begin
    if v = '' then v := pFieldRec(StoreData.Fieldlist[k]).value;
    if (not axp.dbm.gf.IsService) then
    begin
      if (xfld.DataType='n') and (axp.dbm.gf.strtofloatz(v)=0) then result:=true
      else if v='' then result:=true
      else result:=false;
    end else
    begin
      if (xfld.DataType='d') and (v='') then
      begin
         v := axp.dbm.gf.DummyDate;
         result:=false;
      end //else if v='' then result:=true -> SQL like "select case when :attendingid = 0 then 0 when :cbillflg = 'F' then 0" needs to be
          // executed even 'cbilllflg' is empty. open it for backward compatibility.
      else result:=false;
    end;
    if (not axp.dbm.gf.IsService) then  //as discussed with sab
    begin
       result := (result) and (pFieldRec(StoreData.Fieldlist[k]).AutoValue) ;
       if (xfld.ModeofEntry = 'accept') then result := (pFieldRec(StoreData.Fieldlist[k]).AutoValue);
    end;
  end else result := True;
end;

function TValidate.EmptyCheck(xFld:pfld; rowno:integer) : boolean;
var v:String;
    i,k,j:integer;
begin
  v:='';
  k:=-1;
  for I := 0 to xfld.DataRows.count - 1 do begin
    j:=strtoint(xfld.DataRows[i]);
    if pFieldRec(storedata.fieldlist[j]).rowno=rowno then begin
      k:=j;
      break;
    end;
  end;
  if k>-1 then begin
    if v = '' then v := pFieldRec(StoreData.Fieldlist[k]).value;
    if (xfld.DataType='d') and (v='') then
    begin
       v := axp.dbm.gf.DummyDate;
       result:=false;
    end //else if v='' then result:=true -> SQL like "select case when :attendingid = 0 then 0 when :cbillflg = 'F' then 0" needs to be
        // executed even 'cbilllflg' is empty. open it for backward compatibility.
    else result:=false;
  end else result := True;
end;

function TValidate.FieldParentsBound(xfld:pfld; rowno:integer):boolean;
var s, fieldparents:String;
    i:integer;
    f:pfld;
begin
  result:=true;
  if FormLoadPrepareField then exit;
  fieldparents:=trim(sdef.ParentList.Values[xfld.FieldName]);
  axp.dbm.gf.DoDebug.msg('Parents of ' + xfld.fieldname+' are '+FieldParents);
  i:=1;
  while true do begin
    s:=axp.dbm.gf.getnthstring(fieldparents,i);
    if s='' then break;
    If sdef.oldfields.IndexOf(s) > -1 Then Begin
       inc(i);
       continue;
    End;
    inc(i);
    f:=sdef.getfield(s);
    if not assigned(f) then continue;
    if IsFieldEmpty(f, rowno) then begin
      result:=false;
      break;
    end;
  end;
end;

function TValidate.PrepareField(xfld : pFld; RowNo:integer) : pFieldRec ;
var fvalue:String;
    j,k,LinkSearchCol:integer;
    ParentsBound:Boolean;
begin
{It is intended to be called from formload. This function will do the following
1. Load combo values of fields that do not have parents or all parents are bound.
2. Evaluate expressions for which all parents are bound or which do not have parents.
3. Fill autogenerate field values.
}
  axp.dbm.gf.DoDebug.msg('Preparing '+xfld.fieldname+'-'+xfld.modeofentry);
  result:=nil;
  fld:=xfld;
  fvalue:='';

  if xfld.txtSelection then begin
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if (fvalue = '') and (xfld.cexp<>'') and (FieldParentsBound(xfld,RowNo)) then begin
        If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
      end;
      if fvalue <> '' then begin
         Query := xfld.QSelect;
         if ValidatePickList(xfld, fvalue, RowNo) then
         begin
            fvalue := Query.CDS.fields[xfld.searchcol].asstring;
            if xfld.SourceKey then IdValue := xfld.QSelect.CDS.Fields[0].AsFloat
            else idValue := 0;
         end else begin
            fvalue:='';
            idValue := 0;
          end;
          StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', idValue, 0, 0);
          Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
      end;
  end else if xfld.ModeofEntry='accept' then begin
    ParentsBound:=FieldParentsBound(xfld,RowNo);
    if (xfld.cexp<>'') and (ParentsBound) then begin
      If Parser.EvalPrepared(xfld.Exprn) Then begin
        fvalue := Parser.Value;
        if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
        StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end;
    if (assigned(xfld.QSelect)) and (ParentsBound) then begin
      if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
      QueryOpen(xfld.QSelect, RowNo,'0',fvalue);
      if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
      If xfld.DataType = 'c' Then
        fvalue := xfld.QSelect.CDS.Fields[0].asstring
      Else If xfld.DataType = 'n' Then
        fvalue := xfld.QSelect.CDS.fields[0].asstring
      Else If xfld.DataType = 't' Then
        fvalue := xfld.QSelect.CDS.fields[0].asstring //ch1
      Else If xfld.DataType = 'd' Then
        if xfld.QSelect.CDS.fields[0].AsString <> '' then
          fvalue := DateTimeToStr(xfld.QSelect.CDS.fields[0].asdatetime)
        else
          fvalue := xfld.QSelect.CDS.fields[0].AsString;
      if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
      StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end;
  end else if (xfld.ModeOfEntry='select') and (xfld.FromList) and (xfld.exprn>-1) and (FieldParentsBound(xfld,RowNo)) then begin
    If Parser.EvalPrepared(xfld.Exprn) Then begin
      fvalue := Parser.Value;
      if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
      StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end;
  end else if (xfld.ModeOfEntry='select') and (not xfld.FromList) then begin
    fvalue:='';
    idvalue:=0;
    if (xfld.cexp<>'') and (FieldParentsBound(xfld,RowNo)) then begin
      If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
    end;
    if (assigned(xfld.QSelect)) and (FieldParentsBound(xfld,RowNo)) then begin
      if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
      if (FastDataFlag) and ((xfld.Autoselect) or (fvalue<>'')) then
      begin
        QueryOpen(xfld.QSelect, RowNo , '0',fvalue);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) Then begin
          fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
          if xfld.SourceKey then
            IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
        end;
        if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
           fvalue := '';
           idValue := 0;
        end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
      end
      else if not FastDataFlag then
      begin
        QueryOpen(xfld.QSelect, RowNo , '0',fvalue);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) Then begin
          fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
          if xfld.SourceKey then
            IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
        end;
        if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
           fvalue := '';
           idValue := 0;
        end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
      end;
      if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
    end;
    if (fvalue<>'') or ((assigned(xfld.QSelect)) and (xfld.QSelect.Active)) then begin
      StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', IdValue, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end;
  end else if xfld.ModeOfEntry='fill' then begin
    j := Sdef.GetFieldIndex(xfld.LinkField);
    if j=-1 then begin
      axp.dbm.gf.DoDebug.msg('Master field of '+xfld.FieldName+' is not found.');
      exit;
    end;
    if storedata.GetFieldValue(xfld.linkfield, RowNo) <> '' then begin
      Query := pfld(Sdef.flds[j]).QSelect;
      if assigned(Query) and (Query.Active) then begin
        fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
        if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
        StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
      end;
    end;
  end else if (xfld.ModeofEntry = 'autogenerate') and (storedata.GetFieldValue(xfld.FieldName,RowNo)='') then begin
     fvalue := StoreData.GetLastNo(xfld.FieldName,true);
     if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
     StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
     Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
  end else if xfld.ModeOfEntry = 'calculate' then begin
    if (xfld.cexp<>'') and (FieldParentsBound(xfld,RowNo)) then begin
      If Parser.EvalPrepared(xfld.Exprn) Then begin
        fvalue := Parser.Value;
        if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
        StoreData.SubmitValue(xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end;
  end;
  axp.dbm.gf.DoDebug.msg('Field Value - ' + fvalue);
  result:=pFieldRec(StoreData.GetFieldRec(xfld.FieldName, RowNo));
end;

function TValidate.LoadDropDown(xfld : pFld; frec:pFieldRec) : pFieldRec;
var fvalue,OldValue:String;
    j,k, rowno:integer;
    ParentsBound,newvalue:Boolean;
begin
//This function will load only combo values in a DC & evaluate field expressions.
  Result:=frec;
  newvalue:=false;
  if (xfld.ModeOfEntry='select') and (not xfld.FromList) and (not xfld.txtselection) then begin
    axp.dbm.gf.DoDebug.msg('Loading drop down '+xfld.fieldname);
    fld:=xfld;
    if assigned(frec) then begin
      fvalue:=frec.value;
      idvalue:=frec.idvalue;
      rowno:=frec.rowno;
      if (not fld.HasGridParents) and (rowno>1) then exit;
    end else
      rowno:=1;
    ParentsBound:=FieldParentsBound(xfld,rowno);
    if (xfld.cexp<>'') and (fvalue='') and (ParentsBound) then begin
      If Parser.EvalPrepared(xfld.Exprn) Then
      begin
        fvalue := Parser.Value;
        newvalue := true;
      end;
    end;
    if (assigned(xfld.QSelect)) and (ParentsBound) then begin
      if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
      if (FastDataFlag) and ((xfld.Autoselect) or (newvalue))then
      begin
        QueryOpen(xfld.QSelect, rowno , '0',fvalue);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) and (fvalue<>'') Then begin
          fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
          if xfld.SourceKey then
            IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
          newvalue := true;
        end;
        if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
           if xfld.ComponentType = '' then
           begin
             fvalue := '';
             idValue := 0;
           end;
        end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
      end
      else if not FastDataFlag then
      begin
        QueryOpen(xfld.QSelect, rowno , '0',fvalue);
        if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
        If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) and (fvalue<>'') Then begin
          fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
          if xfld.SourceKey then
            IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
          newvalue := true;
        end;
        if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
           if xfld.ComponentType = '' then
           begin
             fvalue := '';
             idValue := 0;
           end;
        end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
      end;

      if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
    end;
    if (assigned(xfld.QSelect)) and (xfld.QSelect.Active) then begin
      if assigned(frec) then OldValue := frec.Value;
      StoreData.SubmitValue(xfld.FieldName, rowno, fvalue, '', IdValue, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      if ((assigned(frec)) and (OldValue<>fvalue)) or (not assigned(frec) and (fvalue<>'')) then
        RefreshFldDepedents(xfld,rowno);
    end;
    axp.dbm.gf.DoDebug.msg('Field Value - ' + fvalue);
    if not assigned(frec) then
      Result:=pFieldRec(storedata.getfieldrec(xfld.fieldname, Rowno))
    else
      Result:=frec;
  end;
end;

function TValidate.EvalExpression(xfld : pFld; frec:pFieldRec) : pFieldRec;
var fvalue : String;
    rowno : integer;
begin
  Result:=nil;
  if assigned(frec) then begin
    fvalue:=frec.value;
    idvalue:=frec.idvalue;
    rowno:=frec.rowno;
  end else
    rowno:=1;
  if (xfld.cexp<>'') and (FieldParentsBound(xfld,rowno)) then begin
    axp.dbm.gf.DoDebug.msg('Evaluating expression of '+xfld.fieldname);
    If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
    StoreData.SubmitValue(xfld.FieldName, rowno, fvalue, '', IdValue, 0, 0);
    Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    axp.dbm.gf.DoDebug.msg('Field Value - ' + fvalue);
    if not assigned(frec) then
      Result:=pFieldRec(storedata.getfieldrec(xfld.fieldname, Rowno))
    else
      Result:=frec;
  end;
end;

Procedure TValidate.RefreshFldDepedents(fd : pFld;rowno : integer) ;
  var j : integer;
  dfd : pFld;
begin
  FldDependentFields:=',';
  if not assigned (fd.Dependents) then exit;
  if loading then exit;
  for j := 0 to fd.Dependents.count-1 do begin
    if (fd.DependentTypes[j+1]='d') or (fd.DependentTypes[j+1]='g') then continue;
    dfd:=StoreData.structdef.getfield(fd.Dependents[j]);
    if not assigned(dfd) then continue;
    if (not pFrm(sdef.frames[fd.FrameNo-1]).popup ) and (pFrm(sdef.frames[dfd.FrameNo-1]).popup) then continue;
    RefreshField(dfd, rowno);
    if (dfd.FrameNo=fd.FrameNo) or (pos(','+dfd.FieldName+',', FldDependentFields) > 0) then continue;
    FldDependentFields:=FldDependentFields + dfd.FieldName+',';
  end;
  delete(FldDependentFields,1,1);
  delete(FldDependentFields, length(FldDependentFields), 1);
end;

function TValidate.PopupAutoFill(PopGrid:pPopGrid; rowno:integer) : boolean;
var sQry:TXDS;
begin
  result := false;
  axp.dbm.gf.DoDebug.msg('Fill popup frame without firm bind'+IntToStr(PopGrid.FrameNo)+' Parent row '+IntToStr(RowNo));
  if not assigned(popGrid.AutoFill) then
  begin
     axp.dbm.gf.DoDebug.msg('Autofill SQL not defined');
     exit;
  end;
  sQry := Axp.dbm.GetXDS(nil);
  sQry.buffered := True;
  sQry.CDS.CommandText := popGrid.AutoFill.Text;
  DynamicSQL := sQry.CDS.CommandText;
  QueryOpen(sQry,RowNo);
  axp.dbm.gf.DoDebug.msg('Record count : '+IntToStr(sQry.cds.RecordCount));
  DeleteDetailRows(popgrid.ParentFrameNo, RowNo);
  GetParentValue(popgrid.FrameNo,RowNo,popgrid.ParentField,ParentList);
  Parser.Registervar('activeprow', 'n', inttostr(rowno));
  CopyQResult(sQry,popgrid);
  sQry.close; FreeAndNil(sQry);
  result := true;
end;

Procedure TValidate.CopyQResult(q:TXDS;popgrid:pPopgrid);
var i,rno,l,k : integer;
    tfld : pfld;
    fname,val : String;
    fm:pFrm;
    foundparent : boolean;
begin
  axp.dbm.gf.DoDebug.msg('Copying result');
  rno := StoreData.RowCount(popgrid.FrameNo);
  ActualRows.Clear;
  for i := 0 to q.CDS.FieldCount - 1 do begin
    tfld := sdef.GetField(q.CDS.Fields[i].FieldName);
    if tfld = nil then
    begin
      axp.dbm.gf.DoDebug.msg(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(sdef.frames[popGrid.FrameNo-1]).Caption);
      Raise Exception.Create(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(sdef.frames[popGrid.FrameNo-1]).Caption);
    end;
    if tfld.FrameNo <> popgrid.FrameNo then
    begin
      axp.dbm.gf.DoDebug.msg(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(sdef.frames[popGrid.FrameNo-1]).Caption);
      Raise Exception.Create(q.CDS.Fields[i].FieldName+' is not found in dc '+pfrm(sdef.frames[popGrid.FrameNo-1]).Caption);
    end;
//      StoreData.SubmitValue(tfld.FieldName,rno,q.CDS.Fields[i].AsString,'',0,0,0);
  end;
  while not q.CDS.Eof do begin
    inc(rno);
    ActualRows.Add(IntToStr(rno));
    InsertParentDetails(rno);
    fm:=sdef.getframe(popgrid.FrameNo);
    for l := fm.StartIndex to fm.StartIndex+fm.FieldCount-1 do begin
       tfld:=pfld(sdef.flds[l]);
       foundparent := false;
       For k := 0 to ParentList.Count-1 do
       begin
           if ParentList.Names[k] = tfld.FieldName then
           begin
             foundparent := true;
             break;
           end;
       end;
       if foundparent  then continue;
       if assigned(q.CDS.FindField(tfld.FieldName)) then
       begin
          StoreData.SubmitValue(tfld.FieldName,rno,q.CDS.FieldByName(tfld.FieldName).AsString,'',0,0,0);
          Parser.RegisterVar(tfld.FieldName,tfld.DataType[1],q.CDS.FieldByName(tfld.FieldName).AsString);
       end else begin
          if (tfld.cexp<>'') and (FieldParentsBound(tfld,rno)) then begin
            If Parser.EvalPrepared(tfld.Exprn) Then begin
              val := Parser.Value;
              SetUsedQuotedStrProp(tfld);
              if tfld.DataType = 'n' then val := FormatNum(val, tfld.Width, tfld.Dec);
              StoreData.SubmitValue(tfld.FieldName, rno, val, '', 0, 0, 0);
              Parser.RegisterVar(tfld.FieldName,tfld.DataType[1],val);
            end;
          end;
       end;
    end;
    if popgrid.AutoFillFld then begin
      fname := 'sub'+Trim(IntToStr(popgrid.frameno))+'_autofill';
      tfld := sdef.GetField(fname);
      if tfld <> nil then
        fname := tfld.FieldName;
      StoreData.SubmitValue(fname,rno,'T','',0,0,0);
    end;
    q.CDS.Next;
  end;
  axp.dbm.gf.DoDebug.msg('Result copied');
end;

procedure TValidate.RefreshPopGrid(Frmno:Integer);
var Actrows : TStringList;
    fm : pfrm;
    tfld : pfld;
    frec : pFieldRec;
    i,j,r,pfrmno,prc,k : integer;
    popgrid : pPopgrid;
    s : String;
begin
  i := pfrm(sdef.frames[FrmNo-1]).Popindex;
  popgrid := pPopgrid(sdef.popgrids[i]);
  s := popgrid.Parent;
  pfrmno := StrToInt(copy(s,3,3));
  prc := storedata.GetRowCount(pfrmno);
  Actrows := TStringList.Create;
  fm:=pFrm(sdef.frames[popgrid.FrameNo-1]);
  for k := 1 to prc do begin
    axp.dbm.gf.DoDebug.msg('Fill popup with firm bind '+ IntToStr(pfrmNo)+ ' Row '+ IntToStr(k));
    RegRow(pfrmno,k);
    Parser.Registervar('activeprow', 'n', inttostr(k));
    GetParentValue(popgrid.FrameNo,k,popgrid.ParentField,ParentList);
    if popgrid.AutoFillFld then
      ParentList.Add('sub'+Trim(inttostr(popgrid.FrameNo))+'_autofill=T');
    ActRows.CommaText := GetActualRows(popgrid.FrameNo,ParentList);
      //Refresh dependents in all rows in pop grid that are related to this parent row.
    for i := 0 to ActRows.Count-1 do begin
      r:=StrToInt(ActRows[i]);
      RegRow(popgrid.FrameNo, r);
      Parser.Registervar('activerow', 'n', inttostr(r));
      for j := fm.StartIndex to fm.StartIndex+fm.FieldCount - 1 do begin
        tfld:=pFld(sdef.flds[j]);
        frec:=storedata.GetFieldRec(tfld.FieldName, r);
        if assigned(frec) then begin
          Parser.RegisterVar(tfld.FieldName, tfld.DataType[1], frec.Value);
        end else
        begin
          if FieldParentsBound(tfld,r) then
            RefreshField(tfld, r);
        end;
      end;
    end;
  end;
end;

Function TValidate.GetErrorStr(m:String;RowNo:Integer):String;
begin
  if Copy(m,1,2) = '--' then
  begin
    if Not DeleteRowOnError then
      DeleteRowOnError := True;
    result := GetFldPrompt(fld, Rowno)+' '+Copy(m,3,Length(m));
  end else
    result := GetFldPrompt(fld, Rowno)+' '+m;
end;

Procedure TValidate.GetAutoGenData(RowNo:Integer);
var aRec : pAutoGenRec;
begin
  New(aRec);
  aRec.Transid := StoreData.Transtype;
  aRec.FieldName := fld.FieldName;
  aRec.Prefix := '';
  aRec.PrefixField := '';
  aRec.Schema := StoreData.CompanyName;
  aRec.TableName := fld.Tablename;
  aRec.RType := 'mod';
  aRec.MemNo := '';
  aRec.Value := FieldValue;
  aRec.RecordId := StoreData.GetParentDocId(fld.FrameNo,RowNo);
  aRec.RecordidUpdated := True;
  aRec.Rowno := RowNo;
  aRec.ParentList := nil;
  aRec.Active := True;
  axp.dbm.gf.AutoGenData.Add(aRec);
end;

function TValidate.FormatNumber(tfld: Pfld; s: String): string;
 var p, decimal, k:integer;
     s1 : AnsiString;
begin
 if (not sdef.quickload) and (not sdef.QuickDataFlag) then
 begin
   Result := s;
   if s='' then exit;
{   decimal := tfld.Dec;
   if (decimal = 0) and (tfld.Dec = tfld.CurDec) then exit;
   p := pos(axp.dbm.gf.LocDecimalSeparator, s);
   if p > 0 then
   begin
     s1 := trim(copy(s, p+1,length(s)));
     if (decimal = length(s1)) and (tfld.Dec = tfld.CurDec) then exit;
   end else if p = 0 then
   begin
     if decimal > 0 then
     begin
        s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0');
        result := s;
     end;
     exit;
   end;}
   if pos('E', s) = 0 then begin
     s := Parser.GetNumber(axp.dbm.gf.RemoveCommas(s));
     s:=floattostrf(axp.dbm.gf.PRound(strtofloat(s),tfld.dec), ffFixed, tfld.Width, tfld.dec);
   end else
     s:=floattostrf(axp.dbm.gf.PRound(strtofloat(s),tfld.dec), ffFixed, tfld.Width, tfld.dec);
   decimal := tfld.Dec;
   if (decimal = 0) and (tfld.Dec = tfld.CurDec) then exit;
   p := pos(axp.dbm.gf.LocDecimalSeparator, s);
   if p = 0 then begin
    if decimal > 0 then
     s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0'); //s := s + '.' + axp.dbm.gf.pad('', decimal, '0')
   end else begin
    if decimal > 0 then
     s := copy(s,1,p) + axp.dbm.gf.pad(copy(s, p+1, decimal), decimal, '0')
    else
     s := copy(s, 1, p-1);
   end;
   Result := s;
 end else
 begin
   if s='' then
   begin
     s := '0';
     decimal := tfld.Dec;
     p := pos(axp.dbm.gf.LocDecimalSeparator, s);
     if p = 0 then begin
      if decimal > 0 then
       s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0'); //s := s + '.' + axp.dbm.gf.pad('', decimal, '0')
     end else begin
      if decimal > 0 then
       s := copy(s,1,p) + axp.dbm.gf.pad(copy(s, p+1, decimal), decimal, '0')
      else
       s := copy(s, 1, p-1);
     end;
     Result := s;
     exit;
   end;
   Result := s;
{   decimal := tfld.Dec;
   if (decimal = 0) and (tfld.Dec = tfld.CurDec) then exit;
   p := pos(axp.dbm.gf.LocDecimalSeparator, s);
   if p > 0 then
   begin
     s1 := trim(copy(s, p+1,length(s)));
     if (decimal = length(s1)) and (tfld.Dec = tfld.CurDec) then exit;
   end else if p = 0 then
   begin
     if decimal > 0 then
     begin
        s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0');
        result := s;
     end;
     exit;
   end;}
   if pos('E', s) = 0 then begin
     s := Parser.GetNumber(axp.dbm.gf.RemoveCommas(s));
     s:=floattostrf(axp.dbm.gf.PRound(strtofloat(s),tfld.dec), ffFixed, tfld.Width, tfld.dec);
   end else
     s:=floattostrf(axp.dbm.gf.PRound(strtofloat(s),tfld.dec), ffFixed, tfld.Width, tfld.dec);
   decimal := tfld.Dec;
   if (decimal = 0) and (tfld.Dec = tfld.CurDec) then exit;
   p := pos(axp.dbm.gf.LocDecimalSeparator, s);
   if p = 0 then begin
    if decimal > 0 then
     s := s + axp.dbm.gf.LocDecimalSeparator + axp.dbm.gf.pad('', decimal, '0'); //s := s + '.' + axp.dbm.gf.pad('', decimal, '0')
   end else begin
    if decimal > 0 then
     s := copy(s,1,p) + axp.dbm.gf.pad(copy(s, p+1, decimal), decimal, '0')
    else
     s := copy(s, 1, p-1);
   end;
   Result := s;
 end;
end;

procedure TValidate.SetUsedQuotedStrProp(xfld : pfld);
begin
  if not axp.dbm.gf.UsedQuotedStr then exit;
  xfld.UsedQuotedStr := True;
  axp.dbm.gf.UsedQuotedStr := False;
end;

function TValidate.Prepare_Selected_Field(xfld : pFld; visibleDCs : AnsiString ; RowNo:integer) : string ;
var fvalue:String;
    dcno : AnsiString;
    j,k,LinkSearchCol:integer;
    ParentsBound,exp_evaluated :Boolean;
    tmp_fld : pFld;
    tmp_val : string;
begin
  axp.dbm.gf.DoDebug.msg('Preparing '+xfld.fieldname+'-'+xfld.modeofentry);
  dependcall := true;
  result:='';
  ErrorStr := '';
  fld:=xfld;
  fvalue:='';
  dcno := '';
  idvalue := 0;
  exp_evaluated := false;
  try
    if xfld.txtSelection then begin
        fvalue:=GetValue(xfld.FieldName , RowNo);
        if (fvalue = '') and (xfld.cexp<>'') and (FieldParentsBound(xfld,RowNo)) then begin
          If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
        end;
        if fvalue <> '' then begin
           Query := xfld.QSelect;
           idValue := 0;
           dcno := 'dc' + vartostr(xfld.FrameNo);
           if (dependcall) or (pos(dcno,visibleDCs) > 0) then
           begin
             if ValidatePickList(xfld, fvalue, RowNo) then
             begin
                if FormLoadPrepareField then
                   if assigned(xfld.QSelect) then axp.dbm.gf.GetUsedGlobalVarsInSqlParams(xfld.QSelect.CDS);
                fvalue := Query.CDS.fields[xfld.searchcol].asstring;
                if xfld.SourceKey then IdValue := xfld.QSelect.CDS.Fields[0].AsFloat
                else idValue := 0;
             end else begin
                fvalue:='';
                idValue := 0;
             end;
           end else begin
             if xfld.SourceKey then
             begin
                if ValidatePickList(xfld, fvalue, RowNo) then
                begin
                  fvalue := Query.CDS.fields[xfld.searchcol].asstring;
                  IdValue := xfld.QSelect.CDS.Fields[0].AsFloat
                end;
             end;
           end;
           if xfld.DataType = 'n' then fvalue := FormatNumber(fvalue);
           StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', idValue, 0, 0);
           Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
        end;
    end else if xfld.ModeofEntry='accept' then begin
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if (xfld.DataType = 'n') and (fvalue <> '') and (axp.dbm.gf.strtofloatz(fvalue) = 0) then fvalue := '';
      if ((fvalue = '') and (xfld.cexp<>'')) then
      begin
        If Parser.EvalPrepared(xfld.Exprn) Then begin
          fvalue := Parser.Value;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
          exp_evaluated := true;
        end;
      end;
      if ((exp_evaluated) or (fvalue = '')) then
      begin
        ParentsBound:=FieldParentsBound(xfld,RowNo);
        if (assigned(xfld.QSelect)) and (ParentsBound) then begin
          if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
          QueryOpen(xfld.QSelect, RowNo,'0',fvalue);
          if FormLoadPrepareField then
             if assigned(xfld.QSelect) then axp.dbm.gf.GetUsedGlobalVarsInSqlParams(xfld.QSelect.CDS);
          if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
          If xfld.DataType = 'c' Then
            fvalue := xfld.QSelect.CDS.Fields[0].asstring
          Else If xfld.DataType = 'n' Then
            fvalue := xfld.QSelect.CDS.fields[0].asstring
          Else If xfld.DataType = 't' Then
            fvalue := xfld.QSelect.CDS.fields[0].asstring //ch1
          Else If xfld.DataType = 'd' Then
            if xfld.QSelect.CDS.fields[0].AsString <> '' then
              fvalue := DateTimeToStr(xfld.QSelect.CDS.fields[0].asdatetime)
            else
              fvalue := xfld.QSelect.CDS.fields[0].AsString;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
        end;
      end;
    end else if (xfld.ModeOfEntry='select') and (xfld.FromList) and (FieldParentsBound(xfld,RowNo)) then begin
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if ((fvalue = '') and (xfld.cexp<>'')) then
      begin
        Parser.EvalPrepared(xfld.Exprn);
        fvalue := Parser.Value;
        if fvalue <> '' then
        begin
          if Sdef.GetFieldIndex(trim(fvalue)) > -1 then
             fvalue := '';
        end;
        if xfld.DataType = 'n' then
        begin
           fvalue := FormatNumber(fvalue)
        end;
        StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end else if (xfld.ModeOfEntry='select') and (not xfld.FromList) then begin
      idvalue:=0;
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if ((fvalue = '') and (xfld.cexp<>'')) or (xfld.AutoSelect) or (xfld.ComponentType<>'') then
      begin
        if (fvalue = '') and (xfld.cexp<>'') then
           If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
        if (fvalue <> '') or (xfld.AutoSelect) or (xfld.ComponentType<>'') then
        begin
          dcno := 'dc' + vartostr(xfld.FrameNo);
          if (assigned(xfld.QSelect)) and (FieldParentsBound(xfld,RowNo)) and ((dependcall) or(pos(dcno,visibleDCs) > 0)) then begin
            if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
            QueryOpen(xfld.QSelect, RowNo , '0',fvalue);
            if FormLoadPrepareField then
               if assigned(xfld.QSelect) then axp.dbm.gf.GetUsedGlobalVarsInSqlParams(xfld.QSelect.CDS);
            if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
            If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) Then begin
              fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
              if xfld.SourceKey then
                IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
            end;
            if ((visibleDCs <> 'loaddata') or ((axp.dbm.gf.AutoselectCompatibility) and (fld.AutoSelect))) and (xfld.ComponentType='') then
            begin
              if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
                 fvalue := '';
                 idValue := 0;
              end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
//              if xfld.ComponentType = '' then xfld.QSelect.close;
            end;
          end;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', IdValue, 0, 0);
          Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
        end;
      end;
    end else if xfld.ModeOfEntry='fill' then begin
      j := Sdef.GetFieldIndex(xfld.LinkField);
      if j=-1 then begin
        axp.dbm.gf.DoDebug.msg('Master field of '+xfld.FieldName+' is not found.');
        exit;
      end;
      if storedata.GetFieldValue(xfld.linkfield, RowNo) <> '' then begin
        Query := pfld(Sdef.flds[j]).QSelect;
        if assigned(Query) and (not Query.Active) then begin
            QueryOpen(Query, RowNo , '0',fvalue);
            if FormLoadPrepareField then
               if assigned(xfld.QSelect) then axp.dbm.gf.GetUsedGlobalVarsInSqlParams(xfld.QSelect.CDS);
          tmp_fld := sdef.GetField(xfld.LinkField);
          if assigned(tmp_fld) then
          begin
            tmp_val := GetValue(xfld.LinkField, RowNo);
            if not Query.CDS.Locate(Query.CDS.Fields[tmp_fld.searchcol].FieldName,tmp_val , [locaseinsensitive]) then begin
               fvalue := '';
            end else fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
          end;
          if xfld.Suggestive  then
          begin
            if (xfld.DataType = 'n') and (fvalue <> '') and (axp.dbm.gf.strtofloatz(fvalue) = 0) then tmp_val := ''
            else if (xfld.DataType = 'n') then
               tmp_val := fvalue
            else
               tmp_val := parser.GetVarValue(xfld.FieldName);
            if tmp_val <> '' then fvalue := tmp_val;
          end;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
        end else
        if assigned(Query) and (Query.Active) then begin
          fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
          if xfld.Suggestive  then
          begin
            if (xfld.DataType = 'n') and (fvalue <> '') and (axp.dbm.gf.strtofloatz(fvalue) = 0) then tmp_val := ''
            else if (xfld.DataType = 'n') then
               tmp_val := fvalue
            else
               tmp_val := parser.GetVarValue(xfld.FieldName);
            if tmp_val <> '' then fvalue := tmp_val;
          end;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
        end;
      end else if xfld.Suggestive  then
      begin
        if (xfld.DataType = 'n') and (fvalue <> '') and (axp.dbm.gf.strtofloatz(fvalue) = 0) then tmp_val := ''
        else if (xfld.DataType = 'n') then
           tmp_val := fvalue
        else
           tmp_val := parser.GetVarValue(xfld.FieldName);
        if tmp_val <> '' then fvalue := tmp_val;
      end;
      if xfld.DataType = 'n' then
      begin
         fvalue := FormatNumber(fvalue)
      end;
      StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
      //if xfld.DataType = 'n' then Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],'0');
    end else if (xfld.ModeofEntry = 'autogenerate') and (storedata.GetFieldValue(xfld.FieldName,RowNo)='') then begin
       if not FormLoadPrepareField then
       begin
         fvalue := StoreData.GetLastNo(xfld.FieldName,true);
         if xfld.DataType = 'n' then
         begin
            fvalue := FormatNumber(fvalue)
         end;
       end;
       StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
       Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end else if xfld.ModeOfEntry = 'calculate' then begin
       if xfld.cexp<>'' then begin
        If Parser.EvalPrepared(xfld.Exprn) Then begin
          fvalue := Parser.Value;
          if xfld.DataType = 'n' then
          begin
             fvalue := FormatNumber(fvalue)
          end;
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
        end;
       end;
    end;
    if FormLoadPrepareField then
       if xfld.cexp<>'' then axp.dbm.gf.GetUsedGlobalVarsInExpr(xfld.cexp);
  except on e:exception do
    begin
       StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
       Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
       ErrorStr := e.Message
    end;
  end;
  axp.dbm.gf.DoDebug.msg('Field Value - ' + fvalue);
  result:=fvalue + '`' + inttostr(rowno) + '`' + floattostr(IdValue);
  if ErrorStr <> '' then axp.dbm.gf.DoDebug.msg('Error in prepare field : ' + ErrorStr);
end;

function TValidate.Prepare_Selected_Field(xfld : pFld; RowNo:integer;fval:string) : String ;
var fvalue,v:String;
    dcno,visibleDCs : AnsiString;
    j,k,LinkSearchCol:integer;
    ParentsBound:Boolean;
begin
  axp.dbm.gf.DoDebug.msg('Preparing '+xfld.fieldname+'-'+xfld.modeofentry);

  result:='';
  ErrorStr := '';
  fld:=xfld;
  fvalue:=fval;
  dcno := '';
  idvalue := 0;
  visibleDCs := sdef.visibleDCs;

  if xfld.txtSelection then begin
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if (fvalue = '') and (xfld.cexp<>'') then begin
        If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
      end;
      if fvalue <> '' then begin
         Query := xfld.QSelect;
         idValue := 0;
         dcno := 'dc' + vartostr(xfld.FrameNo);
         if (dependcall) or (pos(dcno,visibleDCs) > 0) then
         begin
           if ValidatePickList(xfld, fvalue, RowNo) then
           begin
              fvalue := Query.CDS.fields[xfld.searchcol].asstring;
              if xfld.SourceKey then IdValue := xfld.QSelect.CDS.Fields[0].AsFloat
              else idValue := 0;
           end else begin
              fvalue:='';
              idValue := 0;
           end;
         end else begin
           if xfld.SourceKey then
           begin
              if ValidatePickList(xfld, fvalue, RowNo) then
              begin
                fvalue := Query.CDS.fields[xfld.searchcol].asstring;
                IdValue := xfld.QSelect.CDS.Fields[0].AsFloat
              end;
           end;
         end;
         if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
         StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', idValue, 0, 0);
         Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
      end;
  end else if xfld.ModeofEntry='accept' then begin
      fvalue:=GetValue(xfld.FieldName , RowNo);
      if (xfld.DataType = 'n') and (not loading) and (fvalue <> '') and (axp.dbm.gf.strtofloatz(fvalue) = 0) then fvalue := '';
      if ((fvalue = '') and (xfld.cexp<>'')) then
      begin
      If Parser.EvalPrepared(xfld.Exprn) Then begin
        fvalue := Parser.Value;
        if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
        StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end;
//    ParentsBound:=FieldParentsBound(xfld,RowNo);
    if (assigned(xfld.QSelect)) then begin
      if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
      QueryOpen(xfld.QSelect, RowNo,'0',fvalue);
      if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
      If xfld.DataType = 'c' Then
        fvalue := xfld.QSelect.CDS.Fields[0].asstring
      Else If xfld.DataType = 'n' Then
        fvalue := xfld.QSelect.CDS.fields[0].asstring
      Else If xfld.DataType = 't' Then
        fvalue := xfld.QSelect.CDS.fields[0].asstring //ch1
      Else If xfld.DataType = 'd' Then
        if xfld.QSelect.CDS.fields[0].AsString <> '' then
          fvalue := DateTimeToStr(xfld.QSelect.CDS.fields[0].asdatetime)
        else
          fvalue := xfld.QSelect.CDS.fields[0].AsString;
      if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
      StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end;
  end else if (xfld.ModeOfEntry='select') and (xfld.FromList) and (xfld.exprn>-1) then begin
    If Parser.EvalPrepared(xfld.Exprn) Then begin
      fvalue := Parser.Value;
      if fvalue <> '' then
      begin
        if Sdef.GetFieldIndex(trim(fvalue)) > -1 then
           fvalue := '';
      end;
      if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
      StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end;
  end else if (xfld.ModeOfEntry='select') and (not xfld.FromList) then begin
    idvalue:=0;
    fvalue:=GetValue(xfld.FieldName , RowNo);
    if (xfld.cexp<>'') or (xfld.AutoSelect) or (xfld.ComponentType<>'')then
    begin
      If Parser.EvalPrepared(xfld.Exprn) Then fvalue := Parser.Value;
      if (fvalue <> '') or (xfld.AutoSelect) or (xfld.ComponentType<>'') then
      begin
        dcno := 'dc' + vartostr(xfld.FrameNo);
        if (assigned(xfld.QSelect)) and ((dependcall) or(pos(dcno,visibleDCs) > 0)) then begin
          if xfld.DynamicParams then DynamicSQL:=xfld.SQL.Text else DynamicSQL:='';
          QueryOpen(xfld.QSelect, RowNo , '0',fvalue);
          if xQuery<>xfld.QSelect then xfld.QSelect:=xQuery;
          If (xfld.QSelect.CDS.RecordCount = 1) and (xfld.AutoSelect) Then begin
            fvalue := xfld.QSelect.CDS.Fields[xfld.searchcol].AsString;
            if xfld.SourceKey then
              IdValue := xfld.QSelect.CDS.Fields[0].AsFloat;
          end;
          if not xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.searchcol].FieldName, fvalue, [locaseinsensitive]) then begin
             fvalue := '';
             idValue := 0;
          end else if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
        end;
        if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
        StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', IdValue, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end;
  end else if xfld.ModeOfEntry='fill' then begin
    if (Loading) or (dependcall) then
    begin
      axp.dbm.gf.DoDebug.msg('Finding linkfield');
      j := Sdef.GetFieldIndex(xfld.LinkField);
      if j=-1 then begin
        Errorstr := 'Unable to find LinkField for ' + GetFldPrompt(xfld, rowno);
        exit;
      end;
      axp.dbm.gf.DoDebug.msg('Linkfield found and assigning Query from link field');
      Query := pfld(Sdef.flds[j]).QSelect;
      if pfld(sdef.flds[j]).txtSelection then begin
        v:=GetValue(xfld.LinkField, RowNo);
        LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
        if (not Query.cds.Active) or ((Query.cds.Active) and (lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(v))) then begin
          if ValidatePickList(pfld(sdef.flds[j]), v, RowNo) then
            fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring
          else
            fvalue:='';
        end else
            fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
      end else begin
        axp.dbm.gf.DoDebug.msg('Query assigned');
        if assigned(Query) then begin
          LinkSearchCol := pfld(Sdef.flds[j]).searchcol;
          if pfld(Sdef.flds[j]).DynamicParams then DynamicSQL:=pfld(Sdef.flds[j]).SQL.Text else DynamicSQL:='';
          queryopen(query, rowno,'0', storedata.getfieldvalue(xfld.linkfield, rowno));
          if xQuery<>pfld(Sdef.flds[j]).QSelect then pfld(Sdef.flds[j]).QSelect:=xQuery;
          Query:=xQuery;
          fvalue := GetValue(xfld.LinkField, RowNo);
          If (fvalue <> '') Then Begin
            If (lowercase(fvalue) <> lowercase(Query.cds.fields[LinkSearchCol].asstring)) Then
              Query.CDS.Locate(Query.CDS.Fields[LinkSearchcol].FieldName, fvalue, [locaseinsensitive]);
            if lowercase(Query.CDS.Fields[LinkSearchcol].asstring) <> lowercase(fvalue) then begin
              fvalue := ''
            end else
              fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
          End;
        end;
      end;
      if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
      StoreData.SubmitValue(xfld,xfld.FieldName, rowno, fvalue, '', 0, 0, 0);
      Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
    end
    else begin
      j := Sdef.GetFieldIndex(xfld.LinkField);
      if j=-1 then begin
        axp.dbm.gf.DoDebug.msg('Master field of '+xfld.FieldName+' is not found.');
        exit;
      end;
//      if storedata.GetFieldValue(xfld.linkfield, RowNo) <> '' then begin
        Query := pfld(Sdef.flds[j]).QSelect;
        if (assigned(Query) and (Query.Active))  then begin
          fvalue := Query.CDS.fieldbyname(xfld.SourceField).asstring;
          if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
          StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
          Parser.RegisterVar(xfld.FieldName,xfld.DataType[1],fvalue);
        end;
//      end;
    end;
  end else if (xfld.ModeofEntry = 'autogenerate') then begin
     //fvalue := StoreData.GetLastNo(xfld.FieldName,true);
     //if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
     fvalue := storedata.GetFieldValue(xfld.FieldName,RowNo);
     StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
     Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
  end else if xfld.ModeOfEntry = 'calculate' then begin
    if xfld.cexp<>'' then begin
      If Parser.EvalPrepared(xfld.Exprn) Then begin
        fvalue := Parser.Value;
        if (fld.DataType = 'n') and (fvalue <> '') then fvalue := floattostr(axp.dbm.gf.RealRound(strtofloat(fvalue),fld.Dec));
        if xfld.DataType = 'n' then fvalue := FormatNumber(xfld,fvalue);
        StoreData.SubmitValue(xfld,xfld.FieldName, RowNo, fvalue, '', 0, 0, 0);
        Parser.RegisterVar(xfld.FieldName,Char(xfld.DataType[1]),fvalue);
      end;
    end;
  end;
  axp.dbm.gf.DoDebug.msg('Field Value - ' + fvalue);
  result:=fvalue + '`' + inttostr(rowno) + '`' + floattostr(IdValue);
  if ErrorStr <> '' then axp.dbm.gf.DoDebug.msg(ErrorStr);
end;

function TValidate.ValidateSQL(transid,FName,FValue :  String):Boolean;
var
  appname,stype,tcaption,Other,st,JsonData,Output,fdname,stransid,paramname,paramdatatype : string;
  fldordno,dtype,sfrom,savenorm,refresh,autoselect,combobox,exprn,ParamString : String;
  ASBExecSQL : TASBExecSQL;
  sqltext : WideString;
  sxml : IXMLDocument;
  enode,snode : IXMLNode;
  i : Integer;
  paramlist : TStringList;
begin
  result := False;   //field,select from form,select from sql,fillgrid
  try
    try
      if (axp.dbm.gf.actionName = 'isave') or (axp.dbm.gf.actionName = 'usave') or (axp.dbm.gf.actionName = 'nextclk')
          or (axp.dbm.gf.actionName = 'readivdef') then isonline := true;
      sxml := nil; snode := nil; enode := nil;
      ASBExecSQL := TASBExecSQL.create(axp);
      if not isonline then
      begin
        ASBExecSQL.transid := transid;
        ASBExecSQL.isonline := isonline;
        if transid <> '' then
        begin
          sxml := axp.GetStructure('tstructs',transid, '', '');
          snode := sxml.DocumentElement;
          for i := 0 to snode.ChildNodes.Count-1  do
          begin
            enode := snode.ChildNodes[i];
            if (enode.attributes['cat'] = 'field') then
              ASBExecSQL.RegisterVariableFromSXML(enode)
            else
              Continue;
            enode := nil;
          end;
          ASBExecSQL.enode := snode;
          sqltext := ASBExecSQL.checksql(FValue,fdname,'');
          ASBExecSQL.ValidateSQL(sqltext);
        end
        else
        begin
          Paramlist := Tstringlist.create;
          ParamList.CommaText := ivParamList;
          for I := 0 to paramlist.Count-1 do
            begin
              ParamString := paramlist[i];
              paramname := axp.dbm.gf.GetNthString(ParamString, 1, '~');
              paramdatatype := lowercase(axp.dbm.gf.GetNthString(ParamString, 2, '~'));
              ASBExecSQL.RegisterVariableFromSXML(enode,paramname,paramdatatype);
            end;
            FreeAndNil(ParamList);
            ASBExecSQL.cmfrom := 'foriviewdef';
            sqltext := ASBExecSQL.checksql(FValue,fdname,'foriviewdef');
            ASBExecSQL.ValidateSQL(sqltext);
        end;
      end
      else if (axp.dbm.gf.actionName <> 'isave') and (axp.dbm.gf.actionName <> 'nextclk')
           and (axp.dbm.gf.actionName <> 'usave') and (axp.dbm.gf.actionName <> 'readivdef') then
      begin
        fdname := FName;
        if Assigned(parser) then
        begin
          fldordno   := parser.GetVarValue('fldordno');
          dtype      := parser.GetVarValue('datatype');
          sfrom      := lowercase(parser.GetVarValue('modeofentry')); // will be available in mode of entry
          exprn      := parser.GetVarValue('expression');
          stransid   := parser.GetVarValue('stransid');
          if sfrom = '' then
            sfrom := 'fillgrid';
        end;
        ASBExecSQL.transid := stransid;
        if (lowercase(sfrom) <> '') then   //field,select from sql,select from form,fillgrid
        begin
          if (pos(sfrom,'accept,select from form,select from sql,fillgrid') = 0) then
            raise Exception.Create('Not a Valid Input Parameter - SourceFrom');
        end;
        ASBExecSQL.fldname := fdname;
        ASBExecSQL.isonline := isonline;
        ASBExecSQL.datatype := dtype;
        ASBExecSQL.expression := exprn;
        if sfrom <> 'fillgrid' then
          ASBExecSQL.fldordno := strtoint(fldordno)
        else
          ASBExecSQL.fldordno := -1;
        ASBExecSQL.RegisterVariable(stransid);
        if (lowercase(sfrom) = 'select from sql') then  // its for Select from sql
        begin
          ASBExecSQL.savenorm := parser.GetVarValue('savenormalized');
          ASBExecSQL.refresh  := parser.GetVarValue('refreshonsav');;
          ASBExecSQL.autoselect := parser.GetVarValue('autoselect_sql');
          ASBExecSQL.combobox := parser.GetVarValue('combobox_sql');;
          ASBExecSQL.cmfrom := 'select';
          ASBExecSQL.sourcetable := parser.GetVarValue('sourcetable');
          ASBExecSQL.sourcefield := parser.GetVarValue('sourcefield');
          sqltext := parser.GetVarValue('sql_editor_test');
          ASBExecSQL.ValidateSQLforSelect(sqltext);
        end else if (lowercase(sfrom) = 'select from form') then  // its for Select from form
        begin
          ASBExecSQL.savenorm := parser.GetVarValue('savenormalized_form');
          ASBExecSQL.refresh  := parser.GetVarValue('refreshonsave_form');
          ASBExecSQL.autoselect := parser.GetVarValue('autoselect_form');
          ASBExecSQL.combobox := parser.GetVarValue('combobox_form');;
          ASBExecSQL.cmfrom := 'select';
          ASBExecSQL.sourcetable := parser.GetVarValue('tname');
          ASBExecSQL.sourcefield := parser.GetVarValue('selectfield');
          sqltext := parser.GetVarValue('sql_editor_sqltextform');
          ASBExecSQL.ValidateSQLforSelectForm(sqltext);
        end else
        begin // This will execute for field with accept or fillgrid
          if lowercase(sfrom) = 'accept' then
            sqltext := parser.GetVarValue('sql_editor_detail')
          else if lowercase(sfrom) = 'fillgrid' then
            sqltext := parser.GetVarValue('sql_editor_sql');
          sqltext := ASBExecSQL.checksql(sqltext,'','');
          if (sqltext <> '') then
             ASBExecSQL.ValidateSQL(sqltext);
        end;
      end;
      Result := True;
    except on E:Exception do
      begin
        Result := False;
        ErrorStr := E.Message;
      end;
    end;
  Finally
    if Assigned(ASBExecSQL) then
      ASBExecSQL.Destroy;
  End;
end;

function TValidate.ValidateExpression(transid,FName,FValue :  String):Boolean;
var
  appname,stype,tcaption,Other,st,JsonData,Output,fdname : string;
  fldordno,dtype,sfrom,savenorm,refresh,autoselect,combobox,exprn,stransid : String;
  ASBExecSQL : TASBExecSQL;
  sqltext : WideString;
  sxml : IXMLDocument;
  enode,snode : IXMLNode;
  i : Integer;
begin
  result := False;   //field,select from form,select from sql,fillgrid
  try
    try
      if (axp.dbm.gf.actionName = 'isave') or (axp.dbm.gf.actionName = 'usave') or (axp.dbm.gf.actionName = 'nextclk')
          or (axp.dbm.gf.actionName = 'readivdef') then isonline := true;
      sxml := nil; snode := nil; enode := nil;
      ASBExecSQL := TASBExecSQL.create(axp);
      if not isonline then
      begin
        ASBExecSQL.isonline := isonline;
        ASBExecSQL.transid := transid;
        if transid <> '' then
        begin
          sxml := axp.GetStructure('tstructs',transid, '', '');
          snode := sxml.DocumentElement;
          for i := 0 to snode.ChildNodes.Count-1  do
          begin
            enode := snode.ChildNodes[i];
            if (enode.attributes['cat'] = 'field') then
              ASBExecSQL.RegisterVariableFromSXML(enode)
            else
              Continue;
            enode := nil;
          end;
          ASBExecSQL.enode := snode;
          ASBExecSQL.ValidateExpression(FValue);
        end;
      end
      else if (axp.dbm.gf.actionName <> 'isave') and (axp.dbm.gf.actionName <> 'nextclk')
           and (axp.dbm.gf.actionName <> 'usave') and (axp.dbm.gf.actionName <> 'readivdef') then
      begin
        if Assigned(parser) then
        begin
          stransid := parser.GetVarValue('stransid');
        end;
        ASBExecSQL.RegisterVariable(stransid);
        if lowercase(FName) = 'exp_editor_validateexpression' then
          ASBExecSQL.cfrom := 'fgval'
        else if lowercase(FName) = 'exp_editor_footerstring' then
          ASBExecSQL.cfrom := 'fgfootstr';
        ASBExecSQL.ValidateExpression(FValue);
      end;
      Result := True;
    except on E:Exception do
      begin
        Result := False;
        ErrorStr := E.Message;
      end;
    end;
  Finally
    if Assigned(ASBExecSQL) then
      ASBExecSQL.Destroy;
  End;
end;

end.

