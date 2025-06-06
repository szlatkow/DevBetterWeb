﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Events;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Core.Specs;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevBetterWeb.Web.Services;
public class DailyCheckService : BackgroundService
{
  // for debugging purposes so the delay can be changed without making mathematical errors when changing the delay back to one hour
  private const int ONE_HOUR_IN_MILLISECONDS = 3600000;
  private const int DELAY_IN_MILLISECONDS = ONE_HOUR_IN_MILLISECONDS;

  private readonly ILogger<DailyCheckService> _logger;
  private readonly IRepository<DailyCheck> _repository;
	private readonly IMediator _mediator;

	public DailyCheckService(ILogger<DailyCheckService> logger,
    IRepository<DailyCheck> repository,
		IMediator mediator)
  {
    _logger = logger;
    _repository = repository;
		_mediator = mediator;
	}

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      bool dailyCheckRanToday = await DailyCheckRanToday();
      if (!dailyCheckRanToday && DateTime.UtcNow.Hour >= 13)
      {
        _logger.LogInformation("Daily Check running at: {time}", DateTimeOffset.Now);

        RaiseDailyCheckInitiatedEvent();
      }

      await Task.Delay(DELAY_IN_MILLISECONDS, stoppingToken);
    }
  }

  private async Task<bool> DailyCheckRanToday()
  {
    var spec = new DailyCheckByDateSpec(DateTime.Today);

    var todaysDailyCheck = await _repository.FirstOrDefaultAsync(spec);

    if (todaysDailyCheck == null)
    {
      return false;
    }

    return true;
  }

  private async void RaiseDailyCheckInitiatedEvent()
  {
    await _mediator.Publish(new DailyCheckInitiatedEvent());
    _logger.LogInformation("Daily Check Event Raised");

    DailyCheck dailyCheck = new DailyCheck();
    dailyCheck.Date = DateTime.Now;
    await _repository.AddAsync(dailyCheck);
  }
}
