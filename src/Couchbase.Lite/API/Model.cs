﻿//
//  Model.cs
//
//  Author:
//  	Jim Borden  <jim.borden@couchbase.com>
//
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Lite.Serialization;
using LiteCore;
using LiteCore.Interop;
using LiteCore.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Couchbase.Lite
{
    public interface IDocumentModel
    {
        DocumentMetadata Metadata { get; set; }
    }

    public sealed class DocumentMetadata
    {
        public string Id { get; }

        public string Type { get; set; }

        public bool IsDeleted { get; }

        public ulong Sequence { get; }

        internal DocumentMetadata(string id, string type, bool isDeleted, ulong sequence)
        {
            Id = id;
            Type = type;
            IsDeleted = isDeleted;
            Sequence = sequence;
        }
    }

    public sealed unsafe class ModeledDocument<T> : InteropObject
    {
        private readonly Database _db;

        public T Item { get; set; }

        public string Type { get; set; }

        public string Id { get; }

        public IDatabase Db
        {
            get {
                return _db;
            }
        }

        public bool IsDeleted { get; private set; }

        public ulong Sequence { get; }

        private C4Document* _document;

        internal ModeledDocument(T item, Database db, C4Document* native)
        {
            _db = db;
            Item = item;
            Id = native->docID.CreateString();
            Sequence = native->sequence;
            _document = native;
        }

        public bool Save()
        {
            return Save(null, false);
        }

        public bool Delete()
        {
            return Save(null, true);
        }

        private bool Save(IConflictResolver conflictResolver, bool deletion)
        {
            C4Document* newDoc = null;
            var success = Db.InBatch(() =>
            {
                var put = new C4DocPutRequest {
                    docID = _document->docID,
                    history = &_document->revID,
                    historyCount = 1,
                    save = true
                };

                if(deletion) {
                    put.revFlags = C4RevisionFlags.Deleted;
                }

                var body = new FLSliceResult();
                if(!deletion) {
                    body = _db.JsonSerializer.Serialize(Item);
                }

                try {
                    using(var type = new C4String(Type)) {
                        newDoc = (C4Document*)LiteCoreBridge.Check(err =>
                        {
                            var localPut = put;
                            localPut.docType = type.AsC4Slice();
                            return Native.c4doc_put(_db.c4db, &localPut, null, err);
                        });
                    }
                } finally {
                    Native.FLSliceResult_Free(body);
                }

                return true;
            });

            if(!success) {
                Native.c4doc_free(newDoc);
                return success;
            }

            _document = newDoc;
            if(deletion) {
                IsDeleted = true;
            }

            return success;
            
        }

        protected override void Dispose(bool finalizing)
        {
            Native.c4doc_free(_document);
            _document = null;
        }
    }

    public interface ISubdocumentModel
    {
        Subdocument Subdocument { get; set; }
    }

    public interface IPropertyModel
    {
        Property Property { get; set; }
    }
}
