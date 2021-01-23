using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Das.Serializer.Scanners;

namespace Das.Serializer.Json
{
    public class JsonExpress2 : BaseExpress2
    {
        public JsonExpress2(IInstantiator instantiator,
                 IObjectManipulator objectManipulator,
                 ITypeInferrer typeInference,
                 ITypeManipulator types,
                 IStringPrimitiveScanner primitiveScanner,
                 IDynamicTypes dynamicTypes,
                 Char startBlockChar,
                 Char endBlockChar,
                 Char endArrayChar,
                 String typeWrapAttribute,
                 String circularReferenceAttribute,
                 Char[] objectOrStringOrNull,
                 Char[] arrayOrObjectOrNull,
                 Char[] fieldStartChars) : 
            base(instantiator, objectManipulator, typeInference, types, primitiveScanner,
                dynamicTypes, endBlockChar, endArrayChar, typeWrapAttribute, 
                circularReferenceAttribute, fieldStartChars)
        {
        }

        public override IEnumerable<T> DeserializeMany<T>(String txt)
        {
            throw new NotImplementedException();
        }


        protected override NodeTypes GetNodeInstanceType(ref Int32 currentIndex,
                                                         String txt,
                                                         StringBuilder stringBuilder,
                                                         ref Type? specifiedType,
                                                         ref NodeScanState nodeScanState)
        {
            throw new NotImplementedException();
        }

        protected override void HandleEncodingNode(String txt,
                                                   ref Int32 currentIndex,
                                                   StringBuilder stringBuilder,
                                                   ref NodeScanState nodeScanState)
        {
            throw new NotImplementedException();
        }

        //protected override object GetTypeWrappedValue(ref Int32 currentIndex,
        //                                              String txt,
        //                                              Type type,
        //                                              StringBuilder stringBuilder,
        //                                              Object? parent,
        //                                              PropertyInfo? prop,
        //                                              ref Object? root,
        //                                              Object[] ctorValues,
        //                                              ISerializerSettings settings)
        //{
        //    throw new NotImplementedException();
        //}

        protected override void AdvanceScanState(String txt,
                                                 ref Int32 currentIndex,
                                                 StringBuilder stringBuilder,
                                                 ref NodeScanState scanState)
        {
            throw new NotImplementedException();
        }

        protected override void AdvanceScanStateUntil(String txt,
                                                      ref Int32 currentIndex,
                                                      StringBuilder stringBuilder,
                                                      NodeScanState targetState,
                                                      ref NodeScanState scanState)
        {
            throw new NotImplementedException();
        }

        protected override void AdvanceScanStateToNodeOpened(String txt,
                                                             ref Int32 currentIndex,
                                                             StringBuilder stringBuilder,
                                                             ref NodeScanState scanState)
        {
            throw new NotImplementedException();
        }

        protected override void AdvanceScanStateToNodeClose(String txt,
                                                            ref Int32 currentIndex,
                                                            StringBuilder stringBuilder,
                                                            ref NodeScanState scanState)
        {
            throw new NotImplementedException();
        }


        protected override void EnsurePropertyValType(ref Int32 currentIndex,
                                                      String txt,
                                                      StringBuilder stringBuilder,
                                                      ref Type? propvalType)
        {
            throw new NotImplementedException();
        }

        protected override bool IsCollectionHasMoreItems(ref Int32 currentIndex,
                                                         String txt)
        {
            throw new NotImplementedException();
        }

        protected virtual void AdvanceUntilFieldStart(ref Int32 currentIndex,
                                                      String txt)
        {
            throw new NotImplementedException();
        }

        protected override void AdvanceUntilEndOfNode(ref Int32 currentIndex,
                                                      String txt)
        {
            throw new NotImplementedException();
        }

        protected override void LoadNextPrimitive(ref Int32 currentIndex,
                                                  String txt,
                                                  StringBuilder stringBuilder)
        {
            throw new NotImplementedException();
        }

        protected virtual void LoadNextStringValue(ref Int32 currentIndex,
                                                   String txt,
                                                   StringBuilder stringBuilder)
        {
            throw new NotImplementedException();
        }

        protected virtual bool InitializeCollection(ref Int32 currentIndex,
                                                    String txt,
                                                    StringBuilder stringBuilder)
        {
            throw new NotImplementedException();
        }

        protected virtual bool TryGetNextProperty(ref Int32 currentIndex,
                                                  String txt,
                                                  StringBuilder sbString,
                                                  out PropertyInfo prop,
                                                  out Type propValType)
        {
            throw new NotImplementedException();
        }

        protected virtual bool TryLoadNextPropertyName(ref Int32 currentIndex,
                                                       String txt,
                                                       StringBuilder sbString)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetNextString(ref Int32 currentIndex,
                                                 String txt,
                                                 StringBuilder sbString)
        {
            throw new NotImplementedException();
        }
    }
}
