using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using log4net;
using Modbus.Data;
using Modbus.IO;
using Modbus.Message;
using Modbus.Util;

namespace Modbus.Device
{
	public abstract class ModbusSlave : ModbusDevice
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(ModbusSlave));
		private byte _unitID;
		private DataStore _dataStore;
		
		public ModbusSlave(byte unitID, ModbusTransport transport)
			: base(transport)
		{
			_dataStore = DataStoreFactory.CreateDefaultDataStore();
			_unitID = unitID;
		}

		public DataStore DataStore
		{
			get { return _dataStore; }
			set { _dataStore = value; }
		}

		public byte UnitID
		{
			get { return _unitID; }
			set { _unitID = value; }
		}		

		internal static ReadCoilsInputsResponse ReadDiscretes(ReadCoilsInputsRequest request, DiscreteCollection dataSource)
		{
			DiscreteCollection data = DataStore.ReadData<DiscreteCollection, bool>(dataSource, request.StartAddress, request.NumberOfPoints);
			ReadCoilsInputsResponse response = new ReadCoilsInputsResponse(request.FunctionCode, request.SlaveAddress, data.ByteCount, data);

			return response;
		}

		internal static ReadHoldingInputRegistersResponse ReadRegisters(ReadHoldingInputRegistersRequest request, RegisterCollection dataSource)
		{
			RegisterCollection data = DataStore.ReadData<RegisterCollection, ushort>(dataSource, request.StartAddress, request.NumberOfPoints);
			ReadHoldingInputRegistersResponse response = new ReadHoldingInputRegistersResponse(request.FunctionCode, request.SlaveAddress, data.ByteCount, data);

			return response;
		}

		internal static WriteSingleCoilRequestResponse WriteSingleCoil(WriteSingleCoilRequestResponse request, DiscreteCollection dataSource)
		{
			DataStore.WriteData<DiscreteCollection, bool>(new DiscreteCollection(request.Data[0] == Modbus.CoilOn), dataSource, request.StartAddress);
		
			return request;
		}

		internal static WriteMultipleCoilsResponse WriteMultipleCoils(WriteMultipleCoilsRequest request, DiscreteCollection dataSource)
		{
			DataStore.WriteData<DiscreteCollection, bool>(request.Data, dataSource, request.StartAddress);
			WriteMultipleCoilsResponse response = new WriteMultipleCoilsResponse(request.SlaveAddress, request.StartAddress, request.NumberOfPoints);

			return response;
		}

		internal static WriteSingleRegisterRequestResponse WriteSingleRegister(WriteSingleRegisterRequestResponse request, RegisterCollection dataSource)
		{
			DataStore.WriteData<RegisterCollection, ushort>(request.Data, dataSource, request.StartAddress);
			
			return request;
		}

		internal static WriteMultipleRegistersResponse WriteMultipleRegisters(WriteMultipleRegistersRequest request, RegisterCollection dataSource)
		{
			DataStore.WriteData<RegisterCollection, ushort>(request.Data, dataSource, request.StartAddress);
			WriteMultipleRegistersResponse response = new WriteMultipleRegistersResponse(request.SlaveAddress, request.StartAddress, request.NumberOfPoints);

			return response;
		}

		internal IModbusMessage ApplyRequest(IModbusMessage request)
		{
			IModbusMessage response;
			_log.Info(request.ToString());

			switch (request.FunctionCode)
			{
				case Modbus.ReadCoils:
					response = ReadDiscretes((ReadCoilsInputsRequest) request, DataStore.CoilDiscretes);
					break;
				case Modbus.ReadInputs:
					response = ReadDiscretes((ReadCoilsInputsRequest) request, DataStore.InputDiscretes);
					break;
				case Modbus.ReadHoldingRegisters:
					response = ReadRegisters((ReadHoldingInputRegistersRequest) request, DataStore.HoldingRegisters);
					break;
				case Modbus.ReadInputRegisters:
					response = ReadRegisters((ReadHoldingInputRegistersRequest) request, DataStore.InputRegisters);
					break;
				case Modbus.WriteSingleCoil:
					response = WriteSingleCoil((WriteSingleCoilRequestResponse) request, DataStore.CoilDiscretes);
					break;
				case Modbus.WriteSingleRegister:
					response = WriteSingleRegister((WriteSingleRegisterRequestResponse) request, DataStore.HoldingRegisters);
					break;
				case Modbus.WriteMultipleCoils:
					response = WriteMultipleCoils((WriteMultipleCoilsRequest) request, DataStore.CoilDiscretes);
					break;
				case Modbus.WriteMultipleRegisters:
					response = WriteMultipleRegisters((WriteMultipleRegistersRequest) request, DataStore.HoldingRegisters);
					break;
				default:
					string errorMessage = String.Format("Unsupported function code {0}", request.FunctionCode);
					_log.Error(errorMessage);
					throw new ArgumentException(errorMessage, "request");
			}

			return response;
		}

		public abstract void Listen();
	}
}
