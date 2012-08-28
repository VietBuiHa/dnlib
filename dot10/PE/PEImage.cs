﻿using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file
	/// </summary>
	public class PEImage : IPEImage {
		/// <summary>
		/// Use this if the PE file has been loaded into memory by the OS PE file loader
		/// </summary>
		public static readonly IPEType MemoryLayout = new MemoryPEType();

		/// <summary>
		/// Use this if the PE file has a normal structure (eg. it's been read from a file on disk)
		/// </summary>
		public static readonly IPEType FileLayout = new FilePEType();

		BinaryReader reader;
		IStreamCreator streamCreator;
		IPEType peType;
		PEInfo peInfo;

		class FilePEType : IPEType {
			/// <inheritdoc/>
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) {
				return peInfo.ToRVA(offset);
			}

			/// <inheritdoc/>
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) {
				return peInfo.ToFileOffset(rva);
			}
		}

		class MemoryPEType : IPEType {
			/// <inheritdoc/>
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) {
				return new RVA((uint)offset.Value);
			}

			/// <inheritdoc/>
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) {
				return new FileOffset(rva.Value);
			}
		}

		/// <inheritdoc/>
		public ImageDosHeader ImageDosHeader {
			get { return peInfo.ImageDosHeader; }
		}

		/// <inheritdoc/>
		public ImageNTHeaders ImageNTHeaders {
			get { return peInfo.ImageNTHeaders; }
		}

		/// <inheritdoc/>
		public IList<ImageSectionHeader> ImageSectionHeaders {
			get { return peInfo.ImageSectionHeaders; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="streamCreator">The PE stream creator</param>
		/// <param name="peType">One of <see cref="MemoryLayout"/> and <see cref="FileLayout"/></param>
		/// <param name="verify">Verify PE file data</param>
		protected PEImage(IStreamCreator streamCreator, IPEType peType, bool verify) {
			this.streamCreator = streamCreator;
			this.peType = peType;
			ResetReader();
			this.peInfo = new PEInfo(reader, verify);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <param name="mapAsImage">true if we should map it as an executable</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(string filename, bool mapAsImage, bool verify)
			: this(new MemoryMappedFileStreamCreator(filename, mapAsImage), mapAsImage ? MemoryLayout : FileLayout, verify) {
			if (mapAsImage) {
				((MemoryMappedFileStreamCreator)streamCreator).Length = peInfo.GetImageSize();
				ResetReader();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(string filename, bool verify)
			: this(filename, true, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		public PEImage(string filename)
			: this(filename, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="peType">One of <see cref="MemoryLayout"/> and <see cref="FileLayout"/></param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, IPEType peType, bool verify)
			: this(new MemoryStreamCreator(data), peType, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, bool verify)
			: this(data, FileLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		public PEImage(byte[] data)
			: this(data, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="peType">One of <see cref="MemoryLayout"/> and <see cref="FileLayout"/></param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, long length, IPEType peType, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, length), peType, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, long length, bool verify)
			: this(baseAddr, length, MemoryLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		public PEImage(IntPtr baseAddr, long length)
			: this(baseAddr, length, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="peType">One of <see cref="MemoryLayout"/> and <see cref="FileLayout"/></param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, IPEType peType, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, 0x10000), peType, verify) {
			((UnmanagedMemoryStreamCreator)streamCreator).Length = peInfo.GetImageSize();
			ResetReader();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, bool verify)
			: this(baseAddr, MemoryLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		public PEImage(IntPtr baseAddr)
			: this(baseAddr, true) {
		}

		void ResetReader() {
			this.reader = new BinaryReader(streamCreator.CreateFull());
		}

		/// <inheritdoc/>
		public RVA ToRVA(FileOffset offset) {
			return peType.ToRVA(peInfo, offset);
		}

		/// <inheritdoc/>
		public FileOffset ToFileOffset(RVA rva) {
			return peType.ToFileOffset(peInfo, rva);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (streamCreator != null)
				streamCreator.Dispose();
			reader = null;
			streamCreator = null;
			peType = null;
			peInfo = null;
		}

		/// <inheritdoc/>
		public Stream CreateStream(FileOffset offset) {
			if (offset.Value > streamCreator.Length)
				throw new ArgumentOutOfRangeException("offset");
			long length = streamCreator.Length - offset.Value;
			return CreateStream(offset, length);
		}

		/// <inheritdoc/>
		public Stream CreateStream(FileOffset offset, long length) {
			return streamCreator.Create(offset, length);
		}

		/// <inheritdoc/>
		public Stream CreateFullStream() {
			return streamCreator.CreateFull();
		}
	}
}
