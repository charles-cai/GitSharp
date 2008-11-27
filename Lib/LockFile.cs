﻿/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Util;
using Gitty.Exceptions;

namespace Gitty.Lib
{
    [Complete]
    public class LockFile
    {
        private FileInfo refFile;
        private FileInfo lockFile;

        private FileStream os;

        private FileLock fLck;

        private bool haveLock;

        
        public DateTime CommitLastModified { get; private set; }
        public bool NeedStatInformation { get; set; }

        public LockFile(FileInfo file)
        {
            refFile = file;
            lockFile = PathUtil.CombineFilePath(refFile.Directory, refFile.Name + ".lock");
        }

        public bool Lock()
        {
            lockFile.Directory.Create();
            if (lockFile.Exists)
                return false;

            try
            {
                haveLock = true;
                os = lockFile.Create();

                fLck = FileLock.TryLock(os);
                if (fLck == null)
                {
                    // We cannot use unlock() here as this file is not
                    // held by us, but we thought we created it. We must
                    // not delete it, as it belongs to some other process.
                    //
                    haveLock = false;
                    try
                    {
                        os.Close();
                    }
                    catch (Exception)
                    {
                        // Fail by returning haveLck = false.
                    }
                    os = null;
                }
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }

            return haveLock;
        }

        public bool LockForAppend()
        {
            if (!Lock())
                return false;
            CopyCurrentContent();
            return true;
        }


        public void CopyCurrentContent()
        {
            RequireLock();
            try
            {
                FileStream fis = refFile.OpenRead();
                try
                {
                    byte[] buf = new byte[2048];
                    int r;
                    while ((r = fis.Read(buf, 0, buf.Length)) >= 0)
                        os.Write(buf, 0, r);
                }
                finally
                {
                    fis.Close();
                }
            }
            catch (FileNotFoundException)
            {
                // Don't worry about a file that doesn't exist yet, it
                // conceptually has no current content to copy.
                //
            }
            catch (Exception)
            {
                Unlock();
                throw;

            }
        }

        public void Unlock()
        {
            if (os != null)
            {
                if (fLck != null)
                {
                    try
                    {
                        fLck.Release();
                    }
                    catch (IOException)
                    {
                        // Huh?
                    }
                    fLck = null;
                }
                try
                {
                    os.Close();
                }
                catch (IOException)
                {
                    // Ignore this
                }
                os = null;
            }

            if (haveLock)
            {
                haveLock = false;
                lockFile.Delete();
            }
        }

        public bool Commit()
        {
            if (os != null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + refFile + " not closed.");
            }

            SaveStatInformation();
            try
            {
                lockFile.MoveTo(refFile.FullName);
                return true;
            }
            catch (Exception)
            {
                try
                {
                    if (refFile.Exists) refFile.Delete();

                    lockFile.MoveTo(refFile.FullName);
                    return true;
                }
                catch (Exception)
                {
                    
                }
            }
           
            Unlock();
            return false;
        }

        public void Write(byte[] content)
        {
            RequireLock();
            try
            {
                os.Write(content, 0, content.Length);
                os.Flush();
                fLck.Release();
                os.Close();
                os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        public void Write(ObjectId id)
        {
            RequireLock();
            try
            {
                StreamWriter b = new StreamWriter(os);
                id.CopyTo(b);
                b.Write('\n');
                b.Flush();
                fLck.Release();
                b.Close();
                os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        private void RequireLock()
        {
            if (os == null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + refFile + " not held.");
            }
        }

        private void SaveStatInformation()
        {
            if (this.NeedStatInformation)
                this.CommitLastModified = lockFile.LastWriteTime;
        }

        public Stream GetOutputStream()
        {
            throw new NotSupportedException();
        }

        public class FileLock : IDisposable
        {
            public FileStream FileStream { get; private set; }
            public bool Locked { get; private set; }

            private FileLock(FileStream fs)
            {
                this.FileStream = fs;
                this.FileStream.Lock(0, fs.Length);
                this.Locked = true;
            }

            public static FileLock TryLock(FileStream fs)
            {
                try
                {
                    return new FileLock(fs);
                }
                catch (IOException)
                {

                    return null;
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                this.Release();
            }

            public void Release()
            {
                this.FileStream.Unlock(0, this.FileStream.Length);
                this.Locked = false;
            }

            #endregion
        }
        
    }
}
